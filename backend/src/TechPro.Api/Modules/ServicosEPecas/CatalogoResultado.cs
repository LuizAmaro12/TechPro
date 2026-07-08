namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Resultado de escrita do catálogo: um valor ou uma mensagem de erro de
/// negócio (o controller traduz em 400 ProblemDetails).
/// </summary>
public sealed record CatalogoResultado<T>(T? Valor, string? Erro) where T : class
{
    public static CatalogoResultado<T> Ok(T valor) => new(valor, null);
    public static CatalogoResultado<T> Falha(string erro) => new(null, erro);
}
