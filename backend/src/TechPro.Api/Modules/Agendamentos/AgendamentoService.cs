using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Ciclo de vida do agendamento: criação manual e pública, reagendamento,
/// check-in e cancelamento. A disponibilidade é revalidada imediatamente
/// antes de gravar; corrida residual entre requisições simultâneas é risco
/// aceito no MVP (sem lock pessimista).
/// </summary>
public class AgendamentoService(
    TechProDbContext db,
    ITenantProvider tenantProvider,
    DisponibilidadeService disponibilidade,
    ClienteService clientes,
    OrdensServico.OrdemServicoService ordensServico)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    public async Task<List<AgendamentoResponse>> ListarAsync(
        DateOnly? inicio, DateOnly? fim, StatusAgendamento? status)
    {
        var query = db.Agendamentos.Include(a => a.Servico).AsQueryable();
        if (inicio is { } de)
        {
            query = query.Where(a => a.Data >= de);
        }

        if (fim is { } ate)
        {
            query = query.Where(a => a.Data <= ate);
        }

        if (status is { } filtro)
        {
            query = query.Where(a => a.Status == filtro);
        }

        var itens = await query
            .OrderBy(a => a.Data).ThenBy(a => a.HoraInicio)
            .ToListAsync();
        return itens.Select(ParaResponse).ToList();
    }

    public async Task<AgendamentoResponse?> ObterAsync(int id)
    {
        var agendamento = await db.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id);
        return agendamento is null ? null : ParaResponse(agendamento);
    }

    public async Task<CatalogoResultado<AgendamentoResponse>> CriarManualAsync(AgendamentoRequest request)
    {
        Cliente? cliente = null;
        if (request.ClienteId is { } clienteId)
        {
            // GQF: cliente de outro tenant simplesmente "não existe" (anti-IDOR).
            cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId && c.Ativo);
            if (cliente is null)
            {
                return CatalogoResultado<AgendamentoResponse>.Falha("Cliente não encontrado.");
            }
        }

        var nomeContato = ValorOuFallback(request.NomeContato, cliente?.Nome);
        var telefoneContato = ValorOuFallback(request.TelefoneContato, cliente?.Telefone);
        if (nomeContato is null || telefoneContato is null)
        {
            return CatalogoResultado<AgendamentoResponse>.Falha(
                "Informe nome e telefone de contato ou vincule um cliente.");
        }

        var vaga = await ValidarVagaAsync(request.ServicoId, request.Data, request.HoraInicio, somenteOnline: false);
        if (vaga.Erro is not null)
        {
            return CatalogoResultado<AgendamentoResponse>.Falha(vaga.Erro);
        }

        var agendamento = new Agendamento
        {
            TenantId = TenantId,
            ClienteId = cliente?.Id,
            ServicoId = request.ServicoId,
            Data = request.Data,
            HoraInicio = request.HoraInicio,
            HoraFim = vaga.Valor!.HoraFim,
            Origem = OrigemAgendamento.Manual,
            NomeContato = nomeContato,
            TelefoneContato = telefoneContato,
            EmailContato = ValorOuFallback(request.EmailContato, cliente?.Email),
            DescricaoProblema = Normalizar(request.DescricaoProblema),
            AparelhoMarca = Normalizar(request.AparelhoMarca),
            AparelhoModelo = Normalizar(request.AparelhoModelo),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();

        return CatalogoResultado<AgendamentoResponse>.Ok(await CarregarResponseAsync(agendamento.Id));
    }

    /// <summary>Edição/reagendamento — só de agendamentos ainda no status Agendado.</summary>
    public async Task<CatalogoResultado<AgendamentoResponse>?> AtualizarAsync(int id, AgendamentoRequest request)
    {
        var agendamento = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id);
        if (agendamento is null)
        {
            return null;
        }

        if (agendamento.Status != StatusAgendamento.Agendado)
        {
            return CatalogoResultado<AgendamentoResponse>.Falha(
                "Só é possível editar agendamentos que ainda não tiveram check-in nem cancelamento.");
        }

        Cliente? cliente = null;
        if (request.ClienteId is { } clienteId)
        {
            cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId && c.Ativo);
            if (cliente is null)
            {
                return CatalogoResultado<AgendamentoResponse>.Falha("Cliente não encontrado.");
            }
        }

        var nomeContato = ValorOuFallback(request.NomeContato, cliente?.Nome ?? agendamento.NomeContato);
        var telefoneContato = ValorOuFallback(request.TelefoneContato, cliente?.Telefone ?? agendamento.TelefoneContato);

        var vaga = await ValidarVagaAsync(
            request.ServicoId, request.Data, request.HoraInicio, somenteOnline: false, ignorarAgendamentoId: id);
        if (vaga.Erro is not null)
        {
            return CatalogoResultado<AgendamentoResponse>.Falha(vaga.Erro);
        }

        var reagendou = agendamento.Data != request.Data || agendamento.HoraInicio != request.HoraInicio;
        agendamento.ClienteId = cliente?.Id ?? request.ClienteId;
        agendamento.ServicoId = request.ServicoId;
        agendamento.Data = request.Data;
        agendamento.HoraInicio = request.HoraInicio;
        agendamento.HoraFim = vaga.Valor!.HoraFim;
        agendamento.NomeContato = nomeContato!;
        agendamento.TelefoneContato = telefoneContato!;
        agendamento.EmailContato = Normalizar(request.EmailContato) ?? agendamento.EmailContato;
        agendamento.DescricaoProblema = Normalizar(request.DescricaoProblema);
        agendamento.AparelhoMarca = Normalizar(request.AparelhoMarca);
        agendamento.AparelhoModelo = Normalizar(request.AparelhoModelo);
        if (reagendou)
        {
            agendamento.ReagendadoEm = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        return CatalogoResultado<AgendamentoResponse>.Ok(await CarregarResponseAsync(agendamento.Id));
    }

    /// <summary>
    /// Check-in materializa a OS automaticamente (módulo 3) na mesma
    /// transação — ou o aparelho entra no fluxo inteiro, ou nada muda.
    /// </summary>
    public async Task<CatalogoResultado<AgendamentoResponse>?> CheckInAsync(int id, Guid? usuarioId)
    {
        var agendamento = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id);
        if (agendamento is null)
        {
            return null;
        }

        if (agendamento.Status != StatusAgendamento.Agendado)
        {
            return CatalogoResultado<AgendamentoResponse>.Falha(
                "Só é possível fazer check-in de agendamento ativo.");
        }

        await using var transacao = await db.Database.BeginTransactionAsync();
        agendamento.Status = StatusAgendamento.CheckInRealizado;
        await db.SaveChangesAsync();
        await ordensServico.CriarDoAgendamentoAsync(agendamento, usuarioId);
        await transacao.CommitAsync();

        return CatalogoResultado<AgendamentoResponse>.Ok(await CarregarResponseAsync(id));
    }

    public async Task<CatalogoResultado<AgendamentoResponse>?> CancelarAsync(int id, CancelamentoRequest request)
    {
        var agendamento = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id);
        if (agendamento is null)
        {
            return null;
        }

        if (agendamento.Status == StatusAgendamento.Cancelado)
        {
            return CatalogoResultado<AgendamentoResponse>.Falha("Este agendamento já foi cancelado.");
        }

        agendamento.Status = StatusAgendamento.Cancelado;
        agendamento.CanceladoEm = DateTimeOffset.UtcNow;
        agendamento.MotivoCancelamento = Normalizar(request.Motivo);
        await db.SaveChangesAsync();
        return CatalogoResultado<AgendamentoResponse>.Ok(await CarregarResponseAsync(id));
    }

    /// <summary>
    /// Criação pela rota pública (tenant já fixado pelo slug). Vínculo
    /// silencioso: telefone que bate com cliente existente vincula sem expor
    /// nada do cadastro; telefone inédito cria cliente novo no CRM.
    /// </summary>
    public async Task<CatalogoResultado<AgendamentoPublicoResponse>> CriarPublicoAsync(
        AgendamentoPublicoRequest request, string nomeLoja)
    {
        // Tolerância de 1 dia: a data é "hora de parede" da loja e o servidor
        // compara em UTC — sem ela, uma loja UTC-3 não agendaria "hoje" à noite.
        if (request.Data < DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1))
        {
            return CatalogoResultado<AgendamentoPublicoResponse>.Falha(
                "Não é possível agendar em uma data passada.");
        }

        var vaga = await ValidarVagaAsync(request.ServicoId, request.Data, request.HoraInicio, somenteOnline: true);
        if (vaga.Erro is not null)
        {
            return CatalogoResultado<AgendamentoPublicoResponse>.Falha(vaga.Erro);
        }

        var telefone = request.TelefoneContato.Trim();
        var cliente = await clientes.VincularOuCriarPorTelefoneAsync(
            request.NomeContato, telefone, request.EmailContato);

        var agendamento = new Agendamento
        {
            TenantId = TenantId,
            ClienteId = cliente.Id,
            ServicoId = request.ServicoId,
            Data = request.Data,
            HoraInicio = request.HoraInicio,
            HoraFim = vaga.Valor!.HoraFim,
            Origem = OrigemAgendamento.Portal,
            NomeContato = request.NomeContato.Trim(),
            TelefoneContato = telefone,
            EmailContato = Normalizar(request.EmailContato),
            DescricaoProblema = Normalizar(request.DescricaoProblema),
            AparelhoMarca = request.AparelhoMarca.Trim(),
            AparelhoModelo = request.AparelhoModelo.Trim(),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();

        var servicoNome = await db.Servicos
            .Where(s => s.Id == request.ServicoId)
            .Select(s => s.Nome)
            .SingleAsync();
        return CatalogoResultado<AgendamentoPublicoResponse>.Ok(new AgendamentoPublicoResponse(
            agendamento.Id, nomeLoja, servicoNome, agendamento.Data, agendamento.HoraInicio, agendamento.HoraFim));
    }

    // --- Auxiliares --------------------------------------------------------------

    private sealed record Vaga(TimeOnly HoraFim);

    private async Task<CatalogoResultado<Vaga>> ValidarVagaAsync(
        int servicoId, DateOnly data, TimeOnly horaInicio, bool somenteOnline, int? ignorarAgendamentoId = null)
    {
        var resultado = await disponibilidade.CalcularAsync(servicoId, data, somenteOnline, ignorarAgendamentoId);
        if (resultado.Erro is not null)
        {
            return CatalogoResultado<Vaga>.Falha(resultado.Erro);
        }

        if (!resultado.Valor!.HorariosLivres.Contains(horaInicio))
        {
            return CatalogoResultado<Vaga>.Falha("Horário indisponível para este serviço.");
        }

        var fim = horaInicio.Hour * 60 + horaInicio.Minute + resultado.Valor.DuracaoMinutos;
        return CatalogoResultado<Vaga>.Ok(new Vaga(new TimeOnly(fim / 60, fim % 60)));
    }

    private async Task<AgendamentoResponse> CarregarResponseAsync(int id)
    {
        var agendamento = await db.Agendamentos
            .Include(a => a.Servico)
            .SingleAsync(a => a.Id == id);
        return ParaResponse(agendamento);
    }

    private static AgendamentoResponse ParaResponse(Agendamento a) => new(
        a.Id,
        a.Status,
        a.Origem,
        a.ServicoId,
        a.Servico!.Nome,
        a.Data,
        a.HoraInicio,
        a.HoraFim,
        a.ClienteId,
        a.NomeContato,
        a.TelefoneContato,
        a.EmailContato,
        a.DescricaoProblema,
        a.AparelhoMarca,
        a.AparelhoModelo,
        a.CriadoEm,
        a.ReagendadoEm,
        a.CanceladoEm,
        a.MotivoCancelamento);

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? ValorOuFallback(string? valor, string? fallback) =>
        string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
}
