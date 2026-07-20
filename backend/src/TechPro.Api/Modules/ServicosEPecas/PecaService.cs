using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class PecaService(
    TechProDbContext db, ITenantProvider tenantProvider, EstoqueService estoque)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<PaginaResponse<PecaResponse>> ListarAsync(
        string? busca, bool incluirInativas, int pagina, int tamanhoPagina)
    {
        var query = db.Pecas.Include(p => p.Fornecedor).AsQueryable();
        if (!incluirInativas)
        {
            query = query.Where(p => p.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(p => p.Nome.ToLower().Contains(termo));
        }

        var total = await query.CountAsync();
        var itens = await query
            .OrderBy(p => p.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return new PaginaResponse<PecaResponse>(
            itens.Select(ParaResponse).ToList(), total, pagina, tamanhoPagina);
    }

    public async Task<PecaResponse?> ObterAsync(int id)
    {
        var peca = await db.Pecas.Include(p => p.Fornecedor).SingleOrDefaultAsync(p => p.Id == id);
        return peca is null ? null : ParaResponse(peca);
    }

    public async Task<CatalogoResultado<PecaResponse>> CriarAsync(
        PecaRequest request, Guid? usuarioId = null)
    {
        if (!await FornecedorExisteAsync(request.FornecedorId))
        {
            return CatalogoResultado<PecaResponse>.Falha("Fornecedor não encontrado.");
        }

        var peca = new Peca
        {
            TenantId = TenantId,
            CriadoEm = DateTimeOffset.UtcNow,
            Nome = request.Nome.Trim(),
        };
        Aplicar(peca, request);
        db.Pecas.Add(peca);
        await db.SaveChangesAsync();

        // Estoque inicial também é um movimento: sem isso a razão nasceria
        // devendo a diferença e nunca reconciliaria com o saldo.
        if (request.QuantidadeEmEstoque != 0)
        {
            peca.QuantidadeEmEstoque = 0;
            estoque.Registrar(
                peca, TipoMovimentacaoEstoque.Entrada, request.QuantidadeEmEstoque,
                "Estoque inicial no cadastro da peça", peca.CustoUnitario, usuarioId: usuarioId);
            await db.SaveChangesAsync();
        }

        return CatalogoResultado<PecaResponse>.Ok((await ObterAsync(peca.Id))!);
    }

    public async Task<CatalogoResultado<PecaResponse>?> AtualizarAsync(
        int id, PecaRequest request, Guid? usuarioId = null)
    {
        var peca = await db.Pecas.SingleOrDefaultAsync(p => p.Id == id);
        if (peca is null)
        {
            return null;
        }

        if (!await FornecedorExisteAsync(request.FornecedorId))
        {
            return CatalogoResultado<PecaResponse>.Falha("Fornecedor não encontrado.");
        }

        // O formulário do catálogo continua aceitando o saldo, mas a diferença
        // vira um Ajuste registrado — a edição não sobrescreve mais o estoque
        // em silêncio (era o caminho por onde uma baixa concorrente sumia).
        var delta = request.QuantidadeEmEstoque - peca.QuantidadeEmEstoque;
        Aplicar(peca, request);
        if (delta != 0)
        {
            peca.QuantidadeEmEstoque -= delta;
            estoque.Registrar(
                peca, TipoMovimentacaoEstoque.Ajuste, delta,
                "Ajuste pela edição da peça no catálogo", usuarioId: usuarioId);
        }

        await db.SaveChangesAsync();
        return CatalogoResultado<PecaResponse>.Ok((await ObterAsync(id))!);
    }

    public async Task<bool> DesativarAsync(int id)
    {
        var peca = await db.Pecas.SingleOrDefaultAsync(p => p.Id == id);
        if (peca is null)
        {
            return false;
        }

        peca.Ativo = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static void Aplicar(Peca peca, PecaRequest request)
    {
        peca.Nome = request.Nome.Trim();
        peca.Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim();
        peca.CustoUnitario = request.CustoUnitario;
        peca.PrecoVenda = request.PrecoVenda;
        peca.QuantidadeEmEstoque = request.QuantidadeEmEstoque;
        peca.EstoqueMinimo = request.EstoqueMinimo;
        peca.FornecedorId = request.FornecedorId;
        peca.Ativo = request.Ativo;
    }

    // O GQF faz fornecedor de outra empresa "não existir" aqui: referência
    // cruzada de tenant vira 400 de negócio, nunca um vínculo silencioso.
    private async Task<bool> FornecedorExisteAsync(int? fornecedorId) =>
        fornecedorId is null || await db.Fornecedores.AnyAsync(f => f.Id == fornecedorId);

    private static PecaResponse ParaResponse(Peca p) => new(
        p.Id, p.Nome, p.Descricao, p.CustoUnitario, p.PrecoVenda,
        p.QuantidadeEmEstoque, p.EstoqueMinimo,
        p.Fornecedor is null
            ? null
            : new FornecedorResponse(p.Fornecedor.Id, p.Fornecedor.Nome, p.Fornecedor.Contato),
        EstoqueBaixo: p.QuantidadeEmEstoque <= p.EstoqueMinimo,
        p.Ativo);
}
