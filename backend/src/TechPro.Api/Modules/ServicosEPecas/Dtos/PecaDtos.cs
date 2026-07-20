namespace TechPro.Api.Modules.ServicosEPecas.Dtos;

public record PecaRequest(
    string Nome,
    string? Descricao,
    decimal CustoUnitario,
    decimal PrecoVenda,
    int QuantidadeEmEstoque,
    int EstoqueMinimo,
    int? FornecedorId,
    bool Ativo = true);

public record PecaResponse(
    int Id,
    string Nome,
    string? Descricao,
    decimal CustoUnitario,
    decimal PrecoVenda,
    int QuantidadeEmEstoque,
    int EstoqueMinimo,
    FornecedorResponse? Fornecedor,
    bool EstoqueBaixo,
    bool Ativo);

// --- Movimentação de estoque (Fase 2) -----------------------------------------

/// <summary>
/// Movimento manual. <c>Quantidade</c> é sempre **positiva**: o tipo define o
/// sinal, para a UI não precisar ensinar o usuário a digitar número negativo.
/// </summary>
public record MovimentacaoRequest(
    TipoMovimentacaoEstoque Tipo,
    int Quantidade,
    decimal? CustoUnitario,
    string? Motivo);

public record MovimentacaoResponse(
    int Id,
    TipoMovimentacaoEstoque Tipo,
    int Quantidade,
    int SaldoApos,
    decimal? CustoUnitario,
    string? Motivo,
    Guid? OrdemServicoId,
    int? OrdemServicoNumero,
    string? UsuarioNome,
    DateTimeOffset CriadoEm);

// --- Lista de compra ------------------------------------------------------------

public record ItemListaCompraResponse(
    int PecaId,
    string PecaNome,
    int QuantidadeEmEstoque,
    int EstoqueMinimo,
    /// <summary>Quanto comprar para voltar ao mínimo (nunca menos que 1).</summary>
    int SugestaoCompra,
    decimal CustoUnitario,
    decimal CustoEstimado);

public record GrupoListaCompraResponse(
    int? FornecedorId,
    string FornecedorNome,
    List<ItemListaCompraResponse> Itens,
    decimal CustoEstimado);

public record ListaCompraResponse(
    List<GrupoListaCompraResponse> Grupos,
    int TotalDeItens,
    decimal CustoEstimado);
