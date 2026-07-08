namespace TechPro.Api.Shared.Api;

/// <summary>Envelope padrão de listagens paginadas da API.</summary>
public record PaginaResponse<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina);
