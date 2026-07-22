using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Trilha de "quem fez o quê" nas ações sensíveis que **ainda não tinham
/// rastro**: equipe, LGPD e configurações. OS (histórico de etapas), orçamento
/// (trilha de eventos), estoque (razão) e reatribuição já têm as suas — duplicar
/// tudo aqui seria redundância cara de manter sincronizada.
/// </summary>
public class RegistroAuditoria : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid? UsuarioId { get; set; }

    /// <summary>Snapshot: o membro pode ser desativado e a trilha precisa
    /// continuar legível.</summary>
    public required string UsuarioNome { get; set; }

    /// <summary>O que aconteceu, em pt-BR legível ("Membro adicionado").</summary>
    public required string Acao { get; set; }

    /// <summary>Área afetada ("Equipe", "LGPD", "Configurações").</summary>
    public required string Entidade { get; set; }

    public string? EntidadeId { get; set; }

    public string? Detalhe { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
