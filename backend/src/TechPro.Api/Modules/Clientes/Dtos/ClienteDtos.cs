namespace TechPro.Api.Modules.Clientes.Dtos;

public record ClienteRequest(
    string Nome,
    string Telefone,
    string? Email,
    string? Cpf,
    string? Endereco,
    string? Observacoes,
    bool Vip,
    bool ConsentiuComunicacoes,
    int? ClientePrincipalId,
    bool Ativo = true);

public record VinculoResponse(int Id, string Nome);

public record ClienteResponse(
    int Id,
    string Nome,
    string Telefone,
    string? Email,
    string? Cpf,
    string? Endereco,
    string? Observacoes,
    bool Vip,
    bool Ativo,
    VinculoResponse? ClientePrincipal,
    bool ConsentiuComunicacoes,
    DateTimeOffset? ConsentimentoEm,
    int QuantidadeAparelhos);

public record ClienteDetalheResponse(
    int Id,
    string Nome,
    string Telefone,
    string? Email,
    string? Cpf,
    string? Endereco,
    string? Observacoes,
    bool Vip,
    bool Ativo,
    VinculoResponse? ClientePrincipal,
    bool ConsentiuComunicacoes,
    DateTimeOffset? ConsentimentoEm,
    IReadOnlyList<AparelhoResponse> Aparelhos);
