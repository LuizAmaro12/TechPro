namespace TechPro.Api.Modules.Comunicacao.Canais;

/// <summary>
/// Adaptadores padrão: só registram no log, sem enviar nada de verdade.
/// Mantêm dev/teste determinísticos e são a base da abordagem "pronto para
/// plugar" — trocam-se por Evolution/Resend virando a flag de provedor.
/// </summary>
public sealed class LogWhatsAppCanal(ILogger<LogWhatsAppCanal> logger) : ICanalNotificacao
{
    public CanalNotificacao Canal => CanalNotificacao.WhatsApp;

    public Task<ResultadoEnvio> EnviarAsync(
        string destino, string? assunto, string corpo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[WhatsApp simulado] para {Destino}: {Corpo}", destino, corpo);
        return Task.FromResult(ResultadoEnvio.Simulacao());
    }
}

public sealed class LogEmailCanal(ILogger<LogEmailCanal> logger) : ICanalNotificacao
{
    public CanalNotificacao Canal => CanalNotificacao.Email;

    public Task<ResultadoEnvio> EnviarAsync(
        string destino, string? assunto, string corpo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[E-mail simulado] para {Destino} — {Assunto}: {Corpo}",
            destino, assunto, corpo);
        return Task.FromResult(ResultadoEnvio.Simulacao());
    }
}
