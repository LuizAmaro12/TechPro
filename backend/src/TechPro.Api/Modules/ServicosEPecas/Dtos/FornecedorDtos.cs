namespace TechPro.Api.Modules.ServicosEPecas.Dtos;

public record FornecedorRequest(string Nome, string? Contato);

public record FornecedorResponse(int Id, string Nome, string? Contato);
