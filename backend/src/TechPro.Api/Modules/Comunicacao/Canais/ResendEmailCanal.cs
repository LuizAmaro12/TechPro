using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TechPro.Api.Modules.Comunicacao.Canais;

public sealed class ResendOpcoes
{
    public string? ApiKey { get; set; }

    /// <summary>
    /// Remetente. Exige domínio verificado no Resend; sem domínio, só
    /// <c>onboarding@resend.dev</c> funciona (e apenas para o e-mail do dono
    /// da conta). Configurável via Comunicacao:Email:Resend:Remetente.
    /// </summary>
    public string Remetente { get; set; } = "TechPro <onboarding@resend.dev>";
}

/// <summary>
/// E-mail transacional via Resend (doc de stack, seção 7). Corpo em HTML
/// simples. Falha nunca derruba a ação que disparou — vira log.
/// </summary>
public sealed class ResendEmailCanal(
    HttpClient http,
    ResendOpcoes opcoes,
    ILogger<ResendEmailCanal> logger) : ICanalNotificacao
{
    public CanalNotificacao Canal => CanalNotificacao.Email;

    public async Task<ResultadoEnvio> EnviarAsync(
        string destino, string? assunto, string corpo, CancellationToken cancellationToken = default)
    {
        try
        {
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = JsonContent.Create(new
                {
                    from = opcoes.Remetente,
                    to = new[] { destino },
                    subject = assunto ?? "Atualização do seu reparo",
                    html = ParaHtml(corpo),
                }),
            };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opcoes.ApiKey);

            var resposta = await http.SendAsync(requisicao, cancellationToken);
            var conteudo = await resposta.Content.ReadAsStringAsync(cancellationToken);
            if (!resposta.IsSuccessStatusCode)
            {
                return ResultadoEnvio.Falha($"Resend {(int)resposta.StatusCode}: {conteudo}");
            }

            return ResultadoEnvio.Ok(ExtrairId(conteudo));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar e-mail via Resend para {Destino}", destino);
            return ResultadoEnvio.Falha(ex.Message);
        }
    }

    private static string ParaHtml(string corpo) =>
        $"<div style=\"font-family:sans-serif;font-size:15px;color:#14162B;line-height:1.5\">{
            corpo.Replace("\n", "<br>")}</div>";

    private static string? ExtrairId(string conteudo)
    {
        try
        {
            using var doc = JsonDocument.Parse(conteudo);
            return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
