using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Funil único de toda alteração de saldo. Antes desta etapa o estoque era
/// mutado em três pontos sem registro nenhum (consumo na OS, estorno e edição
/// do catálogo) e o saldo não era explicável. Agora **todo** caminho passa por
/// <see cref="Registrar"/>, então esquecer o razão vira erro de compilação em
/// vez de bug silencioso.
/// </summary>
public class EstoqueService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    /// <summary>
    /// Aplica o delta na peça e enfileira a linha do razão. **Não** chama
    /// SaveChanges: quem orquestra decide a fronteira da transação (a baixa da
    /// OS, por exemplo, salva junto com a linha de peça usada).
    /// </summary>
    public MovimentacaoEstoque Registrar(
        Peca peca,
        TipoMovimentacaoEstoque tipo,
        int delta,
        string? motivo = null,
        decimal? custoUnitario = null,
        Guid? ordemServicoId = null,
        Guid? usuarioId = null)
    {
        // Estoque negativo continua permitido (decisão de 2026-07-15: a UI
        // avisa, não bloqueia). O razão apenas registra como se chegou lá.
        peca.QuantidadeEmEstoque += delta;

        var movimento = new MovimentacaoEstoque
        {
            TenantId = TenantId,
            PecaId = peca.Id,
            Tipo = tipo,
            Quantidade = delta,
            SaldoApos = peca.QuantidadeEmEstoque,
            CustoUnitario = custoUnitario,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            OrdemServicoId = ordemServicoId,
            UsuarioId = usuarioId,
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.MovimentacoesEstoque.Add(movimento);
        return movimento;
    }

    // --- Movimento manual ----------------------------------------------------------

    public async Task<CatalogoResultado<MovimentacaoResponse>?> MovimentarAsync(
        int pecaId, MovimentacaoRequest request, Guid? usuarioId)
    {
        var peca = await db.Pecas.SingleOrDefaultAsync(p => p.Id == pecaId);
        if (peca is null)
        {
            return null;
        }

        if (request.Tipo is TipoMovimentacaoEstoque.ConsumoOs or TipoMovimentacaoEstoque.EstornoOs)
        {
            return CatalogoResultado<MovimentacaoResponse>.Falha(
                "Movimentos de OS são gerados pelo fluxo da ordem de serviço.");
        }

        // A UI manda quantidade positiva e o tipo define o sinal — o usuário
        // não precisa aprender a digitar número negativo.
        var delta = request.Tipo switch
        {
            TipoMovimentacaoEstoque.Entrada => request.Quantidade,
            TipoMovimentacaoEstoque.Saida => -request.Quantidade,
            _ => request.Quantidade - peca.QuantidadeEmEstoque,
        };

        if (request.Tipo == TipoMovimentacaoEstoque.Ajuste && delta == 0)
        {
            return CatalogoResultado<MovimentacaoResponse>.Falha(
                "O ajuste informado é igual ao saldo atual.");
        }

        var movimento = Registrar(
            peca, request.Tipo, delta, request.Motivo, request.CustoUnitario, usuarioId: usuarioId);

        // Entrada com custo atualiza o custo da peça: é o preço que a loja
        // acabou de pagar. Base do histórico de preço por fornecedor.
        if (request.Tipo == TipoMovimentacaoEstoque.Entrada && request.CustoUnitario is { } custo)
        {
            peca.CustoUnitario = custo;
        }

        await db.SaveChangesAsync();
        return CatalogoResultado<MovimentacaoResponse>.Ok(
            (await CarregarAsync(pecaId)).First(m => m.Id == movimento.Id));
    }

    public async Task<List<MovimentacaoResponse>?> ListarAsync(int pecaId)
    {
        if (!await db.Pecas.AnyAsync(p => p.Id == pecaId))
        {
            return null;
        }

        return await CarregarAsync(pecaId);
    }

    private async Task<List<MovimentacaoResponse>> CarregarAsync(int pecaId)
    {
        // Ordenação em memória: Sqlite (testes) não ordena DateTimeOffset.
        var movimentos = (await db.MovimentacoesEstoque
                .Where(m => m.PecaId == pecaId)
                .ToListAsync())
            .OrderByDescending(m => m.CriadoEm)
            .ThenByDescending(m => m.Id)
            .ToList();

        var usuarioIds = movimentos
            .Where(m => m.UsuarioId is not null)
            .Select(m => m.UsuarioId!.Value)
            .Distinct()
            .ToList();
        // usuarios é plano de controle (sem GQF) — filtro de tenant explícito.
        var nomes = usuarioIds.Count == 0
            ? []
            : await db.Users
                .Where(u => usuarioIds.Contains(u.Id) && u.TenantId == TenantId)
                .ToDictionaryAsync(u => u.Id, u => u.Nome);

        var ordemIds = movimentos
            .Where(m => m.OrdemServicoId is not null)
            .Select(m => m.OrdemServicoId!.Value)
            .Distinct()
            .ToList();
        var numeros = ordemIds.Count == 0
            ? []
            : await db.OrdensServico
                .Where(o => ordemIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Numero);

        return movimentos.Select(m => new MovimentacaoResponse(
            m.Id,
            m.Tipo,
            m.Quantidade,
            m.SaldoApos,
            m.CustoUnitario,
            m.Motivo,
            m.OrdemServicoId,
            m.OrdemServicoId is { } oid && numeros.TryGetValue(oid, out var numero) ? numero : null,
            m.UsuarioId is { } uid && nomes.TryGetValue(uid, out var nome) ? nome : null,
            m.CriadoEm)).ToList();
    }

    // --- Disponibilidade de peças por serviço ----------------------------------------

    /// <summary>
    /// Para cada serviço, as peças padrão cujo saldo não cobre a quantidade
    /// necessária. Uma consulta em lote (nunca N+1) — a agenda sinaliza a partir
    /// daqui, e a regra fica no estoque para outros consumidores reusarem.
    /// Reflete o saldo real: não desconta o que outros agendamentos comprometem
    /// (reserva de estoque é problema maior, fora do escopo — ver plano).
    /// </summary>
    public async Task<Dictionary<int, List<PecaEmFaltaResponse>>> FaltasPorServicoAsync(
        IEnumerable<int> servicoIds)
    {
        var ids = servicoIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var padroes = await db.Set<ServicoPeca>()
            .Include(sp => sp.Peca)
            .Where(sp => ids.Contains(sp.ServicoId)
                && sp.QuantidadePadrao > 0
                && sp.Peca!.Ativo
                && sp.Peca.QuantidadeEmEstoque < sp.QuantidadePadrao)
            .ToListAsync();

        return padroes
            .GroupBy(sp => sp.ServicoId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(sp => new PecaEmFaltaResponse(
                        sp.PecaId, sp.Peca!.Nome, sp.QuantidadePadrao, sp.Peca.QuantidadeEmEstoque))
                    .OrderBy(p => p.PecaNome)
                    .ToList());
    }

    // --- Lista de compra -------------------------------------------------------------

    /// <summary>
    /// Peças no ou abaixo do mínimo, agrupadas por fornecedor — a loja compra
    /// por fornecedor, não peça a peça. Peça sem fornecedor cai num grupo
    /// próprio em vez de sumir da lista.
    /// </summary>
    public async Task<ListaCompraResponse> ListaDeCompraAsync()
    {
        var pecas = await db.Pecas
            .Include(p => p.Fornecedor)
            .Where(p => p.Ativo && p.QuantidadeEmEstoque <= p.EstoqueMinimo)
            .ToListAsync();

        var grupos = pecas
            .Select(p =>
            {
                // Repor até o mínimo; se já está no mínimo exato, comprar ao
                // menos 1 — senão a peça apareceria na lista sem sugestão.
                var sugestao = Math.Max(1, p.EstoqueMinimo - p.QuantidadeEmEstoque);
                return new
                {
                    Peca = p,
                    Item = new ItemListaCompraResponse(
                        p.Id, p.Nome, p.QuantidadeEmEstoque, p.EstoqueMinimo,
                        sugestao, p.CustoUnitario, sugestao * p.CustoUnitario),
                };
            })
            .GroupBy(x => new
            {
                x.Peca.FornecedorId,
                Nome = x.Peca.Fornecedor?.Nome ?? "Sem fornecedor definido",
            })
            .Select(g => new GrupoListaCompraResponse(
                g.Key.FornecedorId,
                g.Key.Nome,
                g.Select(x => x.Item).OrderBy(i => i.PecaNome).ToList(),
                g.Sum(x => x.Item.CustoEstimado)))
            .OrderBy(g => g.FornecedorNome)
            .ToList();

        return new ListaCompraResponse(
            grupos,
            grupos.Sum(g => g.Itens.Count),
            grupos.Sum(g => g.CustoEstimado));
    }
}
