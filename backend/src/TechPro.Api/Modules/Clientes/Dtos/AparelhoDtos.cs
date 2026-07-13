namespace TechPro.Api.Modules.Clientes.Dtos;

public record AparelhoRequest(
    string Marca,
    string Modelo,
    string? Imei,
    string? SenhaDesbloqueio,
    string? Observacoes,
    bool Ativo = true);

public record AparelhoResponse(
    int Id,
    string Marca,
    string Modelo,
    string? Imei,
    string? SenhaDesbloqueio,
    string? Observacoes,
    bool Ativo);
