using System.Text.Json.Serialization;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Comunicacao;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanalNotificacao
{
    WhatsApp,
    Email,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoEventoComunicacao
{
    AgendamentoConfirmado,
    AgendamentoLembrete,
    OrdemServicoCriada,
    OrcamentoDisponivel,
    OrcamentoAprovado,
    OrcamentoRecusado,
    ProntoParaRetirada,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusMensagem
{
    /// <summary>Entregue ao provedor real (Evolution/Resend).</summary>
    Enviada,

    /// <summary>Adaptador log (sem provedor real configurado) — só registrada.</summary>
    Simulada,

    /// <summary>Cliente sem consentimento — não enviada, mas registrada (LGPD).</summary>
    Suprimida,

    /// <summary>Provedor real recusou/falhou — a ação que disparou não é afetada.</summary>
    Falhou,
}

/// <summary>
/// Registro de auditoria de cada notificação (um por canal). Atende o
/// "registro mínimo das mensagens enviadas" da Fase 1 (fases_MVP item 9) e é
/// a base do inbox unificado por cliente da Fase 2. Fora do escopo offline.
/// </summary>
public class MensagemEnviada : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public int? ClienteId { get; set; }
    public Guid? OrdemServicoId { get; set; }
    public int? AgendamentoId { get; set; }

    public CanalNotificacao Canal { get; set; }
    public required string Destino { get; set; }
    public TipoEventoComunicacao TipoEvento { get; set; }
    public string? Assunto { get; set; }
    public required string Corpo { get; set; }

    public StatusMensagem Status { get; set; }
    public string? Erro { get; set; }
    public string? IdExterno { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
