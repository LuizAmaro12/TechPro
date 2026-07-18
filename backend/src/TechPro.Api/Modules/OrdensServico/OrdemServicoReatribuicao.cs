using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Troca de responsável técnico, **append-only** (como a trilha de orçamento):
/// registra de quem para quem, o motivo e quem fez. É o que responde "quem
/// mexeu no aparelho" se o cliente questionar. Fora do escopo offline — é ato
/// de gestão, não de campo.
/// </summary>
public class OrdemServicoReatribuicao : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    public Guid? DeUsuarioId { get; set; }
    public Guid? ParaUsuarioId { get; set; }

    public required string Motivo { get; set; }

    /// <summary>Quem executou a troca (claim sub).</summary>
    public Guid? PorUsuarioId { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
