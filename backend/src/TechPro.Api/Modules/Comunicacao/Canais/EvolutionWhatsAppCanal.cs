using System.Net.Http.Json;
using System.Text.Json;

namespace TechPro.Api.Modules.Comunicacao.Canais;

public sealed class EvolutionOpcoes
{
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Instancia { get; set; }

    /// <summary>DDI padrão para números sem código de país (Brasil).</summary>
    public string CodigoPais { get; set; } = "55";
}

/// <summary>
/// WhatsApp via Evolution API (decisão do usuário 2026-07-15; ver o plano da
/// etapa para o desvio da Meta Cloud API e o risco de banimento do número).
/// Envia texto por <c>POST {baseUrl}/message/sendText/{instancia}</c> com o
/// header <c>apikey</c>. Falha nunca derruba a ação que disparou — vira log.
/// </summary>
public sealed class EvolutionWhatsAppCanal(
    HttpClient http,
    EvolutionOpcoes opcoes,
    ILogger<EvolutionWhatsAppCanal> logger) : ICanalNotificacao
{
    public CanalNotificacao Canal => CanalNotificacao.WhatsApp;

    public async Task<ResultadoEnvio> EnviarAsync(
        string destino, string? assunto, string corpo, CancellationToken cancellationToken = default)
    {
        try
        {
            var numero = NormalizarNumero(destino);
            var url = $"{opcoes.BaseUrl!.TrimEnd('/')}/message/sendText/{opcoes.Instancia}";

            using var requisicao = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new { number = numero, text = corpo }),
            };
            requisicao.Headers.Add("apikey", opcoes.ApiKey);

            var resposta = await http.SendAsync(requisicao, cancellationToken);
            var conteudo = await resposta.Content.ReadAsStringAsync(cancellationToken);
            if (!resposta.IsSuccessStatusCode)
            {
                return ResultadoEnvio.Falha($"Evolution {(int)resposta.StatusCode}: {conteudo}");
            }

            return ResultadoEnvio.Ok(ExtrairId(conteudo));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar WhatsApp via Evolution para {Destino}", destino);
            return ResultadoEnvio.Falha(ex.Message);
        }
    }

    private string NormalizarNumero(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        return digitos.StartsWith(opcoes.CodigoPais, StringComparison.Ordinal)
            ? digitos
            : opcoes.CodigoPais + digitos;
    }

    private static string? ExtrairId(string conteudo)
    {
        try
        {
            using var doc = JsonDocument.Parse(conteudo);
            return doc.RootElement.TryGetProperty("key", out var key)
                && key.TryGetProperty("id", out var id)
                ? id.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
