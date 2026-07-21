using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Fila de espera: captura a demanda que se perderia quando não há vaga. A
/// conversão reusa <see cref="AgendamentoService.CriarManualAsync"/> — a lógica
/// de agendar (disponibilidade, capacidade, notificação) mora num lugar só.
/// </summary>
public class FilaEsperaService(
    TechProDbContext db,
    ITenantProvider tenantProvider,
    ClienteService clientes,
    AgendamentoService agendamentos)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    // --- Captura -------------------------------------------------------------------

    /// <summary>
    /// Entrada pelo portal: vínculo silencioso por telefone (reusa o helper do
    /// CRM), então o cliente já entra ligado ao cadastro.
    /// </summary>
    public async Task<CatalogoResultado<EntradaFilaEspera>> EntrarPublicoAsync(
        FilaEsperaPublicaRequest request)
    {
        var servico = await db.Servicos
            .FirstOrDefaultAsync(s => s.Id == request.ServicoId && s.Ativo && s.AgendavelOnline);
        if (servico is null)
        {
            return CatalogoResultado<EntradaFilaEspera>.Falha("Serviço não encontrado.");
        }

        var cliente = await clientes.VincularOuCriarPorTelefoneAsync(
            request.NomeContato, request.TelefoneContato, request.EmailContato);

        var entrada = Criar(
            request.ServicoId, cliente.Id, request.NomeContato, request.TelefoneContato,
            request.EmailContato, request.DataPreferida, request.DescricaoProblema,
            request.AparelhoMarca, request.AparelhoModelo, OrigemAgendamento.Portal);
        db.FilaEspera.Add(entrada);
        await db.SaveChangesAsync();
        return CatalogoResultado<EntradaFilaEspera>.Ok(entrada);
    }

    /// <summary>Entrada manual: a loja registra quem ligou.</summary>
    public async Task<CatalogoResultado<FilaEsperaResponse>> EntrarManualAsync(FilaEsperaRequest request)
    {
        if (!await db.Servicos.AnyAsync(s => s.Id == request.ServicoId && s.Ativo))
        {
            return CatalogoResultado<FilaEsperaResponse>.Falha("Serviço não encontrado.");
        }

        Cliente? cliente = null;
        if (request.ClienteId is { } clienteId)
        {
            // GQF: cliente de outro tenant "não existe" (anti-IDOR).
            cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId && c.Ativo);
            if (cliente is null)
            {
                return CatalogoResultado<FilaEsperaResponse>.Falha("Cliente não encontrado.");
            }
        }

        var nome = ValorOuFallback(request.NomeContato, cliente?.Nome);
        var telefone = ValorOuFallback(request.TelefoneContato, cliente?.Telefone);
        if (nome is null || telefone is null)
        {
            return CatalogoResultado<FilaEsperaResponse>.Falha(
                "Informe nome e telefone de contato ou vincule um cliente.");
        }

        var entrada = Criar(
            request.ServicoId, cliente?.Id, nome, telefone,
            ValorOuFallback(request.EmailContato, cliente?.Email), request.DataPreferida,
            request.DescricaoProblema, request.AparelhoMarca, request.AparelhoModelo,
            OrigemAgendamento.Manual);
        db.FilaEspera.Add(entrada);
        await db.SaveChangesAsync();
        return CatalogoResultado<FilaEsperaResponse>.Ok(await CarregarAsync(entrada.Id));
    }

    // --- Consulta ------------------------------------------------------------------

    public async Task<List<FilaEsperaResponse>> ListarAsync(StatusFilaEspera? status)
    {
        var query = db.FilaEspera.Include(f => f.Servico).AsQueryable();
        query = status is { } s
            ? query.Where(f => f.Status == s)
            : query.Where(f => f.Status == StatusFilaEspera.Aguardando);

        // Ordenação em memória: Sqlite (testes) não ordena DateTimeOffset.
        var itens = (await query.ToListAsync())
            .OrderBy(f => f.CriadoEm)
            .ToList();
        return itens.Select(ParaResponse).ToList();
    }

    // --- Resolução -----------------------------------------------------------------

    /// <summary>
    /// Converte em agendamento reusando a criação manual (com todas as
    /// validações). Só de <c>Aguardando</c>: estado terminal não reabre.
    /// </summary>
    public async Task<CatalogoResultado<FilaEsperaResponse>?> ConverterAsync(
        int id, ConverterFilaRequest request)
    {
        var entrada = await db.FilaEspera.FirstOrDefaultAsync(f => f.Id == id);
        if (entrada is null)
        {
            return null;
        }

        if (entrada.Status != StatusFilaEspera.Aguardando)
        {
            return CatalogoResultado<FilaEsperaResponse>.Falha(
                "Esta entrada da fila já foi resolvida.");
        }

        var criado = await agendamentos.CriarManualAsync(new AgendamentoRequest(
            entrada.ServicoId, request.Data, request.HoraInicio, entrada.ClienteId,
            entrada.NomeContato, entrada.TelefoneContato, entrada.EmailContato,
            entrada.DescricaoProblema, entrada.AparelhoMarca, entrada.AparelhoModelo));
        if (criado.Erro is not null)
        {
            // Ex.: horário indisponível — a entrada continua na fila.
            return CatalogoResultado<FilaEsperaResponse>.Falha(criado.Erro);
        }

        entrada.Status = StatusFilaEspera.Convertida;
        entrada.AgendamentoId = criado.Valor!.Id;
        entrada.ResolvidaEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return CatalogoResultado<FilaEsperaResponse>.Ok(await CarregarAsync(id));
    }

    public async Task<CatalogoResultado<FilaEsperaResponse>?> DescartarAsync(
        int id, DescartarFilaRequest request)
    {
        var entrada = await db.FilaEspera.FirstOrDefaultAsync(f => f.Id == id);
        if (entrada is null)
        {
            return null;
        }

        if (entrada.Status != StatusFilaEspera.Aguardando)
        {
            return CatalogoResultado<FilaEsperaResponse>.Falha(
                "Esta entrada da fila já foi resolvida.");
        }

        entrada.Status = StatusFilaEspera.Descartada;
        entrada.MotivoDescarte = Normalizar(request.Motivo);
        entrada.ResolvidaEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return CatalogoResultado<FilaEsperaResponse>.Ok(await CarregarAsync(id));
    }

    // --- Auxiliares ----------------------------------------------------------------

    private EntradaFilaEspera Criar(
        int servicoId, int? clienteId, string nome, string telefone, string? email,
        DateOnly? dataPreferida, string? descricao, string? marca, string? modelo,
        OrigemAgendamento origem) => new()
        {
            TenantId = TenantId,
            ServicoId = servicoId,
            ClienteId = clienteId,
            NomeContato = nome.Trim(),
            TelefoneContato = telefone.Trim(),
            EmailContato = Normalizar(email),
            DataPreferida = dataPreferida,
            DescricaoProblema = Normalizar(descricao),
            AparelhoMarca = Normalizar(marca),
            AparelhoModelo = Normalizar(modelo),
            Origem = origem,
            CriadoEm = DateTimeOffset.UtcNow,
        };

    private async Task<FilaEsperaResponse> CarregarAsync(int id) =>
        ParaResponse(await db.FilaEspera.Include(f => f.Servico).SingleAsync(f => f.Id == id));

    private static FilaEsperaResponse ParaResponse(EntradaFilaEspera f) => new(
        f.Id,
        f.ServicoId,
        f.Servico!.Nome,
        f.ClienteId,
        f.NomeContato,
        f.TelefoneContato,
        f.EmailContato,
        f.DataPreferida,
        f.DescricaoProblema,
        f.AparelhoMarca,
        f.AparelhoModelo,
        f.Origem,
        f.Status,
        f.AgendamentoId,
        f.CriadoEm);

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? ValorOuFallback(string? valor, string? fallback) =>
        string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
}
