namespace TechPro.Api.Modules.Comunicacao.Dtos;

public record MensagemEnviadaResponse(
    int Id,
    CanalNotificacao Canal,
    string Destino,
    TipoEventoComunicacao TipoEvento,
    StatusMensagem Status,
    string? Erro,
    DateTimeOffset CriadoEm);
