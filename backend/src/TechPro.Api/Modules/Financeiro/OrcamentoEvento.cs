using System.Text.Json.Serialization;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Financeiro;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoEventoOrcamento
{
    Enviado,
    Aprovado,
    Recusado,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanalEventoOrcamento
{
    Loja,
    Portal,
}

/// <summary>
/// Trilha de auditoria da aprovação de orçamento (seção 16 do doc de stack,
/// diferencial do branding): quem, quando e o quê — **append-only**, nunca
/// editada nem apagada. Reenvios e respostas repetidas viram novas linhas.
/// </summary>
public class OrcamentoEvento : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public int OrcamentoId { get; set; }

    public TipoEventoOrcamento Tipo { get; set; }
    public CanalEventoOrcamento Canal { get; set; }

    /// <summary>Usuário da loja (claim sub); nulo quando o canal é o portal.</summary>
    public Guid? UsuarioId { get; set; }

    /// <summary>O valor total do orçamento no momento do evento ("o quê").</summary>
    public decimal ValorTotal { get; set; }

    public string? Motivo { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
