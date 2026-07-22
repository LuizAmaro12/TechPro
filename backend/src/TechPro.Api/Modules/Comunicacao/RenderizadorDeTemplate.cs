using System.Text.RegularExpressions;

namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Substituição de <c>{variavel}</c> pelos valores do contexto. Deliberadamente
/// simples: sem condicionais nem laços — template de mensagem curta não precisa
/// de linguagem, e cada recurso extra aqui vira superfície de bug.
/// </summary>
public static partial class RenderizadorDeTemplate
{
    [GeneratedRegex(@"\{([a-zA-Z]+)\}")]
    private static partial Regex Marcador();

    /// <summary>Variáveis usadas no texto, sem repetição.</summary>
    public static IReadOnlyList<string> VariaveisUsadas(string? texto) =>
        string.IsNullOrEmpty(texto)
            ? []
            : Marcador().Matches(texto)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    /// <summary>
    /// Renderiza com o contexto. Variável desconhecida vira string vazia — mas
    /// isso não deve acontecer: a validação na gravação já barra o que não
    /// existe para o evento, então o erro é pego na configuração, não no envio.
    /// </summary>
    public static string Render(string texto, IReadOnlyDictionary<string, string> contexto) =>
        Marcador().Replace(texto, m =>
            contexto.TryGetValue(m.Groups[1].Value, out var valor) ? valor : string.Empty);
}
