using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Financeiro;

/// <summary>
/// Orçamento (com trilha de auditoria append-only, seção 16) e pagamentos da
/// OS. Os status de aprovação e pagamento da OS são derivados daqui — os
/// campos manuais das etapas anteriores saíram da edição.
/// </summary>
public class FinanceiroService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    // --- Orçamento -------------------------------------------------------------

    public async Task<OrcamentoResponse?> ObterOrcamentoAsync(Guid ordemId)
    {
        var orcamento = await db.Orcamentos
            .Include(o => o.Eventos)
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordemId);
        if (orcamento is null)
        {
            return null;
        }

        return await ParaResponseAsync(orcamento);
    }

    /// <summary>
    /// Cria/edita o rascunho. Editar depois de enviado/respondido volta o
    /// status a Rascunho (e o da OS a Pendente) — a trilha preserva o passado.
    /// </summary>
    public async Task<CatalogoResultado<OrcamentoResponse>?> SalvarRascunhoAsync(
        Guid ordemId, OrcamentoRequest request)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        if (OsFinalizada(ordem))
        {
            return CatalogoResultado<OrcamentoResponse>.Falha(
                "OS entregue ou cancelada não tem orçamento editável.");
        }

        var orcamento = await db.Orcamentos
            .Include(o => o.Eventos)
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordemId);
        if (orcamento is null)
        {
            orcamento = new Orcamento
            {
                TenantId = TenantId,
                OrdemServicoId = ordemId,
                CriadoEm = DateTimeOffset.UtcNow,
            };
            db.Orcamentos.Add(orcamento);
        }

        orcamento.ValorMaoDeObra = request.ValorMaoDeObra;
        orcamento.Desconto = request.Desconto;
        if (orcamento.Status != StatusOrcamento.Rascunho)
        {
            orcamento.Status = StatusOrcamento.Rascunho;
            orcamento.MotivoRecusa = null;
            ordem.StatusAprovacao = StatusAprovacaoOrdemServico.Pendente;
        }

        await db.SaveChangesAsync();
        await RecalcularStatusPagamentoAsync(ordem);
        return CatalogoResultado<OrcamentoResponse>.Ok(await ParaResponseAsync(orcamento));
    }

    /// <summary>
    /// Envio: congela o valor das peças, registra na trilha e move a OS para
    /// "Aguardando aprovação" (decisão 2026-07-15: só o envio move etapa).
    /// </summary>
    public async Task<CatalogoResultado<OrcamentoResponse>?> EnviarAsync(Guid ordemId, Guid? usuarioId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        if (OsFinalizada(ordem))
        {
            return CatalogoResultado<OrcamentoResponse>.Falha(
                "OS entregue ou cancelada não envia orçamento.");
        }

        var orcamento = await db.Orcamentos
            .Include(o => o.Eventos)
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordemId);
        if (orcamento is null)
        {
            return CatalogoResultado<OrcamentoResponse>.Falha(
                "Salve o orçamento antes de enviar.");
        }

        orcamento.ValorPecas = await SomaPecasAsync(ordemId);
        orcamento.Status = StatusOrcamento.Enviado;
        orcamento.EnviadoEm = DateTimeOffset.UtcNow;
        orcamento.RespondidoEm = null;
        orcamento.MotivoRecusa = null;
        ordem.StatusAprovacao = StatusAprovacaoOrdemServico.Pendente;

        RegistrarEvento(orcamento, TipoEventoOrcamento.Enviado,
            CanalEventoOrcamento.Loja, usuarioId, TotalDe(orcamento), null);

        if (ordem.Etapa != EtapaOrdemServico.AguardandoAprovacao)
        {
            OrdemServicoService.RegistrarHistoricoEtapa(
                db, TenantId, ordem, ordem.Etapa,
                EtapaOrdemServico.AguardandoAprovacao, usuarioId, "Orçamento enviado");
            ordem.Etapa = EtapaOrdemServico.AguardandoAprovacao;
        }

        await db.SaveChangesAsync();
        await RecalcularStatusPagamentoAsync(ordem);
        return CatalogoResultado<OrcamentoResponse>.Ok(await ParaResponseAsync(orcamento));
    }

    public async Task<CatalogoResultado<OrcamentoResponse>?> ResponderAsync(
        Guid ordemId, bool aprovado, string? motivo, Guid? usuarioId, CanalEventoOrcamento canal)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        var orcamento = await db.Orcamentos
            .Include(o => o.Eventos)
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordemId);
        if (orcamento is null)
        {
            return null;
        }

        if (orcamento.Status != StatusOrcamento.Enviado)
        {
            return CatalogoResultado<OrcamentoResponse>.Falha(
                "Só um orçamento enviado (e ainda sem resposta) pode ser aprovado ou recusado.");
        }

        motivo = Normalizar(motivo);
        orcamento.Status = aprovado ? StatusOrcamento.Aprovado : StatusOrcamento.Recusado;
        orcamento.RespondidoEm = DateTimeOffset.UtcNow;
        orcamento.MotivoRecusa = aprovado ? null : motivo;
        ordem.StatusAprovacao = aprovado
            ? StatusAprovacaoOrdemServico.Aprovado
            : StatusAprovacaoOrdemServico.Recusado;

        RegistrarEvento(orcamento,
            aprovado ? TipoEventoOrcamento.Aprovado : TipoEventoOrcamento.Recusado,
            canal, usuarioId, TotalDe(orcamento), motivo);

        await db.SaveChangesAsync();
        await RecalcularStatusPagamentoAsync(ordem);
        return CatalogoResultado<OrcamentoResponse>.Ok(await ParaResponseAsync(orcamento));
    }

    public async Task<OrcamentoPublicoResponse?> ObterOrcamentoPublicoAsync(Guid ordemId)
    {
        var orcamento = await db.Orcamentos
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordemId);
        // Rascunho é interno — o cliente só vê depois do envio.
        if (orcamento is null || orcamento.Status == StatusOrcamento.Rascunho)
        {
            return null;
        }

        return new OrcamentoPublicoResponse(
            orcamento.ValorMaoDeObra,
            orcamento.ValorPecas,
            orcamento.Desconto,
            TotalDe(orcamento),
            orcamento.Status,
            orcamento.EnviadoEm,
            orcamento.RespondidoEm);
    }

    // --- Pagamentos --------------------------------------------------------------

    public async Task<ResumoPagamentosResponse?> ObterResumoPagamentosAsync(Guid ordemId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        return await MontarResumoAsync(ordem);
    }

    public async Task<CatalogoResultado<ResumoPagamentosResponse>?> RegistrarPagamentoAsync(
        Guid ordemId, PagamentoRequest request, Guid? usuarioId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        db.Pagamentos.Add(new Pagamento
        {
            TenantId = TenantId,
            OrdemServicoId = ordemId,
            Valor = request.Valor,
            Forma = request.Forma,
            Observacao = Normalizar(request.Observacao),
            RegistradoPorUsuarioId = usuarioId,
            CriadoEm = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        await RecalcularStatusPagamentoAsync(ordem);
        return CatalogoResultado<ResumoPagamentosResponse>.Ok(await MontarResumoAsync(ordem));
    }

    public async Task<CatalogoResultado<ResumoPagamentosResponse>?> RemoverPagamentoAsync(
        Guid ordemId, int pagamentoId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        var pagamento = await db.Pagamentos
            .FirstOrDefaultAsync(p => p.Id == pagamentoId && p.OrdemServicoId == ordemId);
        if (ordem is null || pagamento is null)
        {
            return null;
        }

        db.Pagamentos.Remove(pagamento);
        await db.SaveChangesAsync();
        await RecalcularStatusPagamentoAsync(ordem);
        return CatalogoResultado<ResumoPagamentosResponse>.Ok(await MontarResumoAsync(ordem));
    }

    // --- Auxiliares ----------------------------------------------------------------

    private static bool OsFinalizada(OrdemServico ordem) =>
        ordem.Etapa is EtapaOrdemServico.Entregue or EtapaOrdemServico.Cancelado;

    // Somas em memória: o Sqlite dos testes não agrega decimal no servidor e
    // as linhas por OS são poucas.
    private async Task<decimal> SomaPecasAsync(Guid ordemId) =>
        (await db.OrdensServicoPecas
            .Where(l => l.OrdemServicoId == ordemId && l.DeletedAt == null)
            .Select(l => new { l.PrecoVendaNoUso, l.Quantidade })
            .ToListAsync())
        .Sum(l => l.PrecoVendaNoUso * l.Quantidade);

    private static decimal TotalDe(Orcamento orcamento) =>
        orcamento.ValorMaoDeObra + orcamento.ValorPecas - orcamento.Desconto;

    /// <summary>Rascunho calcula peças ao vivo; enviado usa o valor congelado.</summary>
    private async Task<(decimal Pecas, decimal Total)> ValoresAtuaisAsync(Orcamento orcamento)
    {
        var pecas = orcamento.Status == StatusOrcamento.Rascunho
            ? await SomaPecasAsync(orcamento.OrdemServicoId)
            : orcamento.ValorPecas;
        return (pecas, orcamento.ValorMaoDeObra + pecas - orcamento.Desconto);
    }

    private void RegistrarEvento(
        Orcamento orcamento, TipoEventoOrcamento tipo, CanalEventoOrcamento canal,
        Guid? usuarioId, decimal valorTotal, string? motivo) =>
        db.OrcamentoEventos.Add(new OrcamentoEvento
        {
            TenantId = TenantId,
            OrcamentoId = orcamento.Id,
            Tipo = tipo,
            Canal = canal,
            UsuarioId = usuarioId,
            ValorTotal = valorTotal,
            Motivo = motivo,
            CriadoEm = DateTimeOffset.UtcNow,
        });

    /// <summary>
    /// Deriva o status de pagamento da OS: soma dos pagamentos vs. total do
    /// orçamento. Sem orçamento (ou total zero), pagamento marca no máximo
    /// Parcial — não há total para quitar.
    /// </summary>
    private async Task RecalcularStatusPagamentoAsync(OrdemServico ordem)
    {
        var totalPago = (await db.Pagamentos
                .Where(p => p.OrdemServicoId == ordem.Id)
                .Select(p => p.Valor)
                .ToListAsync())
            .Sum();

        var orcamento = await db.Orcamentos
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordem.Id);
        var total = orcamento is null ? (decimal?)null : (await ValoresAtuaisAsync(orcamento)).Total;

        ordem.StatusPagamento = totalPago <= 0
            ? StatusPagamentoOrdemServico.NaoPago
            : total is > 0 && totalPago >= total
                ? StatusPagamentoOrdemServico.Pago
                : StatusPagamentoOrdemServico.Parcial;
        await db.SaveChangesAsync();
    }

    private async Task<ResumoPagamentosResponse> MontarResumoAsync(OrdemServico ordem)
    {
        var pagamentos = await db.Pagamentos
            .Where(p => p.OrdemServicoId == ordem.Id)
            .ToListAsync();

        var usuarioIds = pagamentos
            .Where(p => p.RegistradoPorUsuarioId is not null)
            .Select(p => p.RegistradoPorUsuarioId!.Value)
            .Distinct()
            .ToList();
        var nomes = await db.Users
            .Where(u => usuarioIds.Contains(u.Id) && u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.Nome);

        var orcamento = await db.Orcamentos
            .FirstOrDefaultAsync(o => o.OrdemServicoId == ordem.Id);
        var total = orcamento is null ? (decimal?)null : (await ValoresAtuaisAsync(orcamento)).Total;
        var totalPago = pagamentos.Sum(p => p.Valor);

        return new ResumoPagamentosResponse(
            pagamentos
                .OrderBy(p => p.CriadoEm)
                .Select(p => new PagamentoResponse(
                    p.Id, p.Valor, p.Forma, p.Observacao,
                    p.RegistradoPorUsuarioId is { } uid && nomes.TryGetValue(uid, out var nome)
                        ? nome
                        : null,
                    p.CriadoEm))
                .ToList(),
            totalPago,
            total,
            total is { } t ? t - totalPago : null,
            ordem.StatusPagamento);
    }

    private async Task<OrcamentoResponse> ParaResponseAsync(Orcamento orcamento)
    {
        var usuarioIds = orcamento.Eventos
            .Where(e => e.UsuarioId is not null)
            .Select(e => e.UsuarioId!.Value)
            .Distinct()
            .ToList();
        var nomes = await db.Users
            .Where(u => usuarioIds.Contains(u.Id) && u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.Nome);

        var (pecas, total) = await ValoresAtuaisAsync(orcamento);
        return new OrcamentoResponse(
            orcamento.Id,
            orcamento.Status,
            orcamento.ValorMaoDeObra,
            pecas,
            orcamento.Desconto,
            total,
            orcamento.MotivoRecusa,
            orcamento.EnviadoEm,
            orcamento.RespondidoEm,
            orcamento.Eventos
                .OrderBy(e => e.CriadoEm)
                .Select(e => new OrcamentoEventoResponse(
                    e.Tipo, e.Canal,
                    e.UsuarioId is { } uid && nomes.TryGetValue(uid, out var nome) ? nome : null,
                    e.ValorTotal, e.Motivo, e.CriadoEm))
                .ToList());
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
