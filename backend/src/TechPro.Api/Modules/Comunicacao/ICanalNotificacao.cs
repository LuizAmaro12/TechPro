namespace TechPro.Api.Modules.Comunicacao;

/// <summary>Resultado de uma tentativa de envio por um canal.</summary>
public record ResultadoEnvio(bool Sucesso, bool Simulado, string? Erro, string? IdExterno)
{
    public static ResultadoEnvio Ok(string? idExterno = null) => new(true, false, null, idExterno);
    public static ResultadoEnvio Simulacao() => new(true, true, null, null);
    public static ResultadoEnvio Falha(string erro) => new(false, false, erro, null);
}

/// <summary>
/// Um canal de notificação (WhatsApp, e-mail). O provedor concreto é escolhido
/// por configuração (adaptador log por padrão; Evolution/Resend quando ligados)
/// — o resto do sistema fala só com esta interface.
/// </summary>
public interface ICanalNotificacao
{
    CanalNotificacao Canal { get; }

    Task<ResultadoEnvio> EnviarAsync(
        string destino, string? assunto, string corpo, CancellationToken cancellationToken = default);
}
