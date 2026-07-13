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
