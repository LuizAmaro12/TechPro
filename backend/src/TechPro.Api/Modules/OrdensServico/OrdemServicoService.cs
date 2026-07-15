using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Ciclo de vida da OS: criação manual (idempotente) e por conversão de
/// agendamento no check-in, edição de campos de gestão, movimentação de
/// etapa com trilha append-only e sincronização por delta (contrato da
/// Fase 2). Soft-delete: queries de negócio filtram DeletedAt explicitamente;
/// o sync inclui as lápides.
/// </summary>
public class OrdemServicoService(
    TechProDbContext db,
    ITenantProvider tenantProvider,
    ClienteService clientes,
    OrdemServicoPecaService pecas,
    Financeiro.FinanceiroService financeiro)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    public async Task<List<OrdemServicoResponse>> ListarAsync(
        EtapaOrdemServico? etapa, string? busca, Guid? responsavelId, bool incluirFinalizadas)
    {
        var query = QueryCompleta().Where(o => o.DeletedAt == null);

        if (etapa is { } filtroEtapa)
        {
            query = query.Where(o => o.Etapa == filtroEtapa);
        }
        else if (!incluirFinalizadas)
        {
            query = query.Where(o =>
                o.Etapa != EtapaOrdemServico.Entregue && o.Etapa != EtapaOrdemServico.Cancelado);
        }

        if (responsavelId is { } tecnico)
        {
            query = query.Where(o => o.ResponsavelTecnicoId == tecnico);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = int.TryParse(termo, out var numero)
                ? query.Where(o => o.Numero == numero)
                : query.Where(o =>
                    o.Cliente!.Nome.Contains(termo) || o.Cliente.Telefone.Contains(termo));
        }

        // Numero cresce com a criação (por tenant) e é traduzível em qualquer
        // provedor — Sqlite dos testes não ordena por DateTimeOffset.
        var ordens = await query
            .OrderByDescending(o => o.Numero)
            .ToListAsync();
        return ordens.Select(ParaResponse).ToList();
    }

    public async Task<OrdemServicoDetalheResponse?> ObterAsync(Guid id)
    {
        var ordem = await QueryCompleta()
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        // Ordenação em memória: Sqlite não ordena DateTimeOffset no servidor.
        var historico = (await db.HistoricosEtapaOrdemServico
                .Where(h => h.OrdemServicoId == id && h.DeletedAt == null)
                .ToListAsync())
            .OrderBy(h => h.CriadoEm)
            .ToList();

        // Usuários são plano de controle (sem GQF) — resolve nomes à mão.
        var usuarioIds = historico
            .Where(h => h.UsuarioId is not null)
            .Select(h => h.UsuarioId!.Value)
            .Distinct()
            .ToList();
        var nomes = await db.Users
            .Where(u => usuarioIds.Contains(u.Id) && u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.Nome);

        return new OrdemServicoDetalheResponse(
            ParaResponse(ordem),
            historico.Select(h => new HistoricoEtapaResponse(
                h.DeEtapa,
                h.ParaEtapa,
                h.UsuarioId is { } uid && nomes.TryGetValue(uid, out var nome) ? nome : null,
                h.Motivo,
                h.CriadoEm)).ToList(),
            await pecas.CarregarLinhasAsync(id),
            await financeiro.ObterOrcamentoAsync(id),
            await financeiro.ObterResumoPagamentosAsync(id));
    }

    public async Task<CatalogoResultado<OrdemServicoResponse>> CriarAsync(
        OrdemServicoRequest request, string? chaveIdempotencia, Guid? usuarioId)
    {
        // Idempotência: reenvio da mesma mutação devolve a OS já criada.
        if (!string.IsNullOrWhiteSpace(chaveIdempotencia))
        {
            var existente = await QueryCompleta()
                .FirstOrDefaultAsync(o => o.ChaveIdempotencia == chaveIdempotencia);
            if (existente is not null)
            {
                return CatalogoResultado<OrdemServicoResponse>.Ok(ParaResponse(existente));
            }
        }

        var cliente = await db.Clientes
            .FirstOrDefaultAsync(c => c.Id == request.ClienteId && c.Ativo);
        if (cliente is null)
        {
            return CatalogoResultado<OrdemServicoResponse>.Falha("Cliente não encontrado.");
        }

        var servico = await db.Servicos
            .FirstOrDefaultAsync(s => s.Id == request.ServicoId && s.Ativo);
        if (servico is null)
        {
            return CatalogoResultado<OrdemServicoResponse>.Falha("Serviço não encontrado.");
        }

        var validacao = await ValidarReferenciasAsync(
            request.AparelhoId, request.ClienteId, request.ResponsavelTecnicoId);
        if (validacao is not null)
        {
            return CatalogoResultado<OrdemServicoResponse>.Falha(validacao);
        }

        var ordem = new OrdemServico
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Numero = await ProximoNumeroAsync(),
            ClienteId = cliente.Id,
            AparelhoId = request.AparelhoId,
            AparelhoMarca = Normalizar(request.AparelhoMarca),
            AparelhoModelo = Normalizar(request.AparelhoModelo),
            ServicoId = servico.Id,
            Prioridade = request.Prioridade,
            PrazoEstimado = request.PrazoEstimado,
            ResponsavelTecnicoId = request.ResponsavelTecnicoId,
            DescricaoProblema = Normalizar(request.DescricaoProblema),
            Observacoes = Normalizar(request.Observacoes),
            CodigoAcompanhamento = GerarCodigoAcompanhamento(),
            ChaveIdempotencia = Normalizar(chaveIdempotencia),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.OrdensServico.Add(ordem);
        RegistrarHistorico(ordem, deEtapa: null, ordem.Etapa, usuarioId, motivo: null);
        await db.SaveChangesAsync();

        return CatalogoResultado<OrdemServicoResponse>.Ok(await CarregarResponseAsync(ordem.Id));
    }

    /// <summary>
    /// Conversão automática no check-in do agendamento (gancho da etapa 4).
    /// Agendamento sem cliente vinculado usa o mesmo vínculo silencioso por
    /// telefone do portal. Chamado dentro da transação do check-in.
    /// </summary>
    public async Task<OrdemServico> CriarDoAgendamentoAsync(Agendamento agendamento, Guid? usuarioId)
    {
        var existente = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.AgendamentoId == agendamento.Id && o.DeletedAt == null);
        if (existente is not null)
        {
            return existente;
        }

        var clienteId = agendamento.ClienteId
            ?? (await clientes.VincularOuCriarPorTelefoneAsync(
                agendamento.NomeContato, agendamento.TelefoneContato, agendamento.EmailContato)).Id;

        var ordem = new OrdemServico
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Numero = await ProximoNumeroAsync(),
            ClienteId = clienteId,
            AparelhoMarca = agendamento.AparelhoMarca,
            AparelhoModelo = agendamento.AparelhoModelo,
            ServicoId = agendamento.ServicoId,
            AgendamentoId = agendamento.Id,
            DescricaoProblema = agendamento.DescricaoProblema,
            CodigoAcompanhamento = GerarCodigoAcompanhamento(),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.OrdensServico.Add(ordem);
        RegistrarHistorico(ordem, deEtapa: null, ordem.Etapa, usuarioId, motivo: null);
        await db.SaveChangesAsync();
        return ordem;
    }

    public async Task<CatalogoResultado<OrdemServicoResponse>?> AtualizarAsync(
        Guid id, OrdemServicoAtualizacaoRequest request)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        var validacao = await ValidarReferenciasAsync(
            request.AparelhoId, ordem.ClienteId, request.ResponsavelTecnicoId);
        if (validacao is not null)
        {
            return CatalogoResultado<OrdemServicoResponse>.Falha(validacao);
        }

        ordem.AparelhoId = request.AparelhoId;
        ordem.AparelhoMarca = Normalizar(request.AparelhoMarca);
        ordem.AparelhoModelo = Normalizar(request.AparelhoModelo);
        ordem.DescricaoProblema = Normalizar(request.DescricaoProblema);
        ordem.Prioridade = request.Prioridade;
        ordem.PrazoEstimado = request.PrazoEstimado;
        ordem.ResponsavelTecnicoId = request.ResponsavelTecnicoId;
        ordem.Observacoes = Normalizar(request.Observacoes);
        await db.SaveChangesAsync();

        return CatalogoResultado<OrdemServicoResponse>.Ok(await CarregarResponseAsync(id));
    }

    /// <summary>
    /// Movimentação livre entre etapas (correções permitidas) — toda mudança
    /// vai para a trilha. Cancelamento grava o motivo na própria OS.
    /// </summary>
    public async Task<CatalogoResultado<OrdemServicoResponse>?> MudarEtapaAsync(
        Guid id, MudancaEtapaRequest request, Guid? usuarioId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        if (ordem.Etapa == request.ParaEtapa)
        {
            return CatalogoResultado<OrdemServicoResponse>.Falha("A OS já está nessa etapa.");
        }

        RegistrarHistorico(ordem, ordem.Etapa, request.ParaEtapa, usuarioId, Normalizar(request.Motivo));
        ordem.Etapa = request.ParaEtapa;
        if (request.ParaEtapa == EtapaOrdemServico.Cancelado)
        {
            ordem.MotivoCancelamento = Normalizar(request.Motivo);
        }

        await db.SaveChangesAsync();
        return CatalogoResultado<OrdemServicoResponse>.Ok(await CarregarResponseAsync(id));
    }

    /// <summary>Delta desde a marca d'água — inclui soft-deletados (lápides).</summary>
    public async Task<OrdensServicoSyncResponse> SincronizarAsync(DateTimeOffset? since)
    {
        var agora = DateTimeOffset.UtcNow;

        // O filtro do since entra por branch (o padrão "since == null ||" com
        // nullable não traduz no Sqlite); a comparação simples traduz como
        // texto ISO-8601 UTC, que ordena cronologicamente. ORDER BY de
        // DateTimeOffset também não traduz — fica em memória.
        var queryOrdens = QueryCompleta();
        var queryHistorico = db.HistoricosEtapaOrdemServico.AsQueryable();
        var queryPecas = db.OrdensServicoPecas.AsQueryable();
        if (since is { } marca)
        {
            queryOrdens = queryOrdens.Where(o => o.UpdatedAt > marca);
            queryHistorico = queryHistorico.Where(h => h.UpdatedAt > marca);
            queryPecas = queryPecas.Where(p => p.UpdatedAt > marca);
        }

        var ordens = (await queryOrdens.ToListAsync())
            .OrderBy(o => o.UpdatedAt)
            .ToList();
        var historico = (await queryHistorico.ToListAsync())
            .OrderBy(h => h.UpdatedAt)
            .ToList();
        var pecasUtilizadas = (await queryPecas.ToListAsync())
            .OrderBy(p => p.UpdatedAt)
            .ToList();

        return new OrdensServicoSyncResponse(
            ordens.Select(o => new OrdemServicoSyncItem(ParaResponse(o), o.DeletedAt)).ToList(),
            historico.Select(h => new HistoricoSyncItem(
                h.Id, h.OrdemServicoId, h.DeEtapa, h.ParaEtapa, h.Motivo,
                h.CriadoEm, h.UpdatedAt, h.DeletedAt)).ToList(),
            pecasUtilizadas.Select(p => new PecaUsadaSyncItem(
                p.Id, p.OrdemServicoId, p.PecaId, p.Quantidade,
                p.CustoUnitarioNoUso, p.PrecoVendaNoUso,
                p.CriadoEm, p.UpdatedAt, p.DeletedAt)).ToList(),
            agora);
    }

    // --- Auxiliares ---------------------------------------------------------------

    private IQueryable<OrdemServico> QueryCompleta() => db.OrdensServico
        .Include(o => o.Cliente)
        .Include(o => o.Servico)
        .Include(o => o.ResponsavelTecnico);

    /// <summary>
    /// Aparelho precisa pertencer ao cliente da OS; responsável precisa ser
    /// usuário do tenant (usuarios não têm GQF — validação anti-IDOR manual).
    /// </summary>
    private async Task<string?> ValidarReferenciasAsync(
        int? aparelhoId, int clienteId, Guid? responsavelId)
    {
        if (aparelhoId is { } aid &&
            !await db.Aparelhos.AnyAsync(a => a.Id == aid && a.ClienteId == clienteId && a.Ativo))
        {
            return "Aparelho não encontrado para este cliente.";
        }

        if (responsavelId is { } rid &&
            !await db.Users.AnyAsync(u => u.Id == rid && u.TenantId == TenantId))
        {
            return "Responsável técnico não encontrado.";
        }

        return null;
    }

    private async Task<int> ProximoNumeroAsync() =>
        (await db.OrdensServico.MaxAsync(o => (int?)o.Numero) ?? 0) + 1;

    private void RegistrarHistorico(
        OrdemServico ordem,
        EtapaOrdemServico? deEtapa,
        EtapaOrdemServico paraEtapa,
        Guid? usuarioId,
        string? motivo) =>
        RegistrarHistoricoEtapa(db, TenantId, ordem, deEtapa, paraEtapa, usuarioId, motivo);

    /// <summary>
    /// Compartilhado com o módulo Financeiro (enviar orçamento move a OS para
    /// Aguardando aprovação) sem criar ciclo de dependência entre os services.
    /// </summary>
    internal static void RegistrarHistoricoEtapa(
        TechProDbContext db,
        Guid tenantId,
        OrdemServico ordem,
        EtapaOrdemServico? deEtapa,
        EtapaOrdemServico paraEtapa,
        Guid? usuarioId,
        string? motivo)
    {
        db.HistoricosEtapaOrdemServico.Add(new OrdemServicoHistoricoEtapa
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrdemServicoId = ordem.Id,
            DeEtapa = deEtapa,
            ParaEtapa = paraEtapa,
            UsuarioId = usuarioId,
            Motivo = motivo,
            CriadoEm = DateTimeOffset.UtcNow,
        });
    }

    private static string GerarCodigoAcompanhamento() =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));

    private async Task<OrdemServicoResponse> CarregarResponseAsync(Guid id) =>
        ParaResponse(await QueryCompleta().SingleAsync(o => o.Id == id));

    private static OrdemServicoResponse ParaResponse(OrdemServico o) => new(
        o.Id,
        o.Numero,
        o.Etapa,
        o.Prioridade,
        o.PrazoEstimado,
        o.ClienteId,
        o.Cliente!.Nome,
        o.Cliente.Telefone,
        o.ServicoId,
        o.Servico!.Nome,
        o.AparelhoId,
        o.AparelhoMarca,
        o.AparelhoModelo,
        o.ResponsavelTecnicoId,
        o.ResponsavelTecnico?.Nome,
        o.StatusPagamento,
        o.StatusAprovacao,
        o.DescricaoProblema,
        o.Observacoes,
        o.MotivoCancelamento,
        o.CodigoAcompanhamento,
        o.AgendamentoId,
        o.CriadoEm,
        o.UpdatedAt);

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
