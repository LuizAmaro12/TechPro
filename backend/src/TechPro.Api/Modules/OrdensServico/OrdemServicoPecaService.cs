using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Peças utilizadas na OS — a baixa automática do módulo 7. Adicionar baixa
/// o estoque na hora (negativo permitido, com aviso — decisão 2026-07-15) e
/// congela custo/preço; remover devolve ao estoque via soft-delete (lápide
/// sincronizável). OS finalizada não recebe nem devolve peças.
/// </summary>
public class OrdemServicoPecaService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    public async Task<List<PecaUsadaResponse>?> ListarAsync(Guid ordemId)
    {
        if (!await db.OrdensServico.AnyAsync(o => o.Id == ordemId && o.DeletedAt == null))
        {
            return null;
        }

        return await CarregarLinhasAsync(ordemId);
    }

    public async Task<CatalogoResultado<PecaUsadaResponse>?> AdicionarAsync(
        Guid ordemId, PecaUsadaRequest request)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        if (OsFinalizada(ordem))
        {
            return CatalogoResultado<PecaUsadaResponse>.Falha(
                "OS entregue ou cancelada não recebe peças.");
        }

        // GQF: peça de outro tenant "não existe" (anti-IDOR → 400).
        var peca = await db.Pecas.FirstOrDefaultAsync(p => p.Id == request.PecaId && p.Ativo);
        if (peca is null)
        {
            return CatalogoResultado<PecaUsadaResponse>.Falha("Peça não encontrada.");
        }

        var linha = CriarLinha(ordem, peca, request.Quantidade);
        await db.SaveChangesAsync();
        return CatalogoResultado<PecaUsadaResponse>.Ok(ParaResponse(linha, peca));
    }

    /// <summary>
    /// Aplica as "peças normalmente utilizadas" do serviço da OS (catálogo),
    /// pulando as que já estão na OS — idempotente.
    /// </summary>
    public async Task<CatalogoResultado<List<PecaUsadaResponse>>?> AplicarPadraoAsync(Guid ordemId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        if (OsFinalizada(ordem))
        {
            return CatalogoResultado<List<PecaUsadaResponse>>.Falha(
                "OS entregue ou cancelada não recebe peças.");
        }

        var padrao = await db.Set<ServicoPeca>()
            .Include(sp => sp.Peca)
            .Where(sp => sp.ServicoId == ordem.ServicoId)
            .ToListAsync();

        var jaUsadas = await db.OrdensServicoPecas
            .Where(l => l.OrdemServicoId == ordemId && l.DeletedAt == null)
            .Select(l => l.PecaId)
            .ToListAsync();

        var adicionadas = new List<(OrdemServicoPeca Linha, Peca Peca)>();
        foreach (var item in padrao.Where(sp =>
                     sp.Peca is { Ativo: true } && !jaUsadas.Contains(sp.PecaId)))
        {
            adicionadas.Add((CriarLinha(ordem, item.Peca!, item.QuantidadePadrao), item.Peca!));
        }

        await db.SaveChangesAsync();
        return CatalogoResultado<List<PecaUsadaResponse>>.Ok(
            adicionadas.Select(a => ParaResponse(a.Linha, a.Peca)).ToList());
    }

    public async Task<CatalogoResultado<PecaUsadaResponse>?> RemoverAsync(
        Guid ordemId, Guid pecaUsadaId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        var linha = await db.OrdensServicoPecas
            .Include(l => l.Peca)
            .FirstOrDefaultAsync(l =>
                l.Id == pecaUsadaId && l.OrdemServicoId == ordemId && l.DeletedAt == null);
        if (ordem is null || linha is null)
        {
            return null;
        }

        if (OsFinalizada(ordem))
        {
            return CatalogoResultado<PecaUsadaResponse>.Falha(
                "OS entregue ou cancelada não devolve peças.");
        }

        // Lápide (o app offline sincroniza a remoção) + devolução ao estoque.
        linha.DeletedAt = DateTimeOffset.UtcNow;
        linha.Peca!.QuantidadeEmEstoque += linha.Quantidade;
        await db.SaveChangesAsync();
        return CatalogoResultado<PecaUsadaResponse>.Ok(ParaResponse(linha, linha.Peca));
    }

    // --- Auxiliares ----------------------------------------------------------------

    private OrdemServicoPeca CriarLinha(OrdemServico ordem, Peca peca, int quantidade)
    {
        var linha = new OrdemServicoPeca
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrdemServicoId = ordem.Id,
            PecaId = peca.Id,
            Quantidade = quantidade,
            CustoUnitarioNoUso = peca.CustoUnitario,
            PrecoVendaNoUso = peca.PrecoVenda,
            CriadoEm = DateTimeOffset.UtcNow,
        };
        peca.QuantidadeEmEstoque -= quantidade;
        db.OrdensServicoPecas.Add(linha);
        return linha;
    }

    internal async Task<List<PecaUsadaResponse>> CarregarLinhasAsync(Guid ordemId)
    {
        var linhas = await db.OrdensServicoPecas
            .Include(l => l.Peca)
            .Where(l => l.OrdemServicoId == ordemId && l.DeletedAt == null)
            .ToListAsync();
        return linhas
            .OrderBy(l => l.CriadoEm)
            .Select(l => ParaResponse(l, l.Peca!))
            .ToList();
    }

    private static bool OsFinalizada(OrdemServico ordem) =>
        ordem.Etapa is EtapaOrdemServico.Entregue or EtapaOrdemServico.Cancelado;

    private static PecaUsadaResponse ParaResponse(OrdemServicoPeca linha, Peca peca) => new(
        linha.Id,
        peca.Id,
        peca.Nome,
        linha.Quantidade,
        linha.CustoUnitarioNoUso,
        linha.PrecoVendaNoUso,
        peca.QuantidadeEmEstoque,
        peca.QuantidadeEmEstoque <= peca.EstoqueMinimo,
        peca.QuantidadeEmEstoque < 0,
        linha.CriadoEm);
}
