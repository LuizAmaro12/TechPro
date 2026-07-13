using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Clientes;

/// <summary>
/// Aparelho de um cliente. A senha de desbloqueio é dado sensível guardado
/// por decisão aprovada (2026-07-12) — candidata a criptografia de campo se
/// surgir exigência; a criptografia em repouso do provedor já cobre o disco.
/// </summary>
public class Aparelho : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public int ClienteId { get; set; }
    public required string Marca { get; set; }
    public required string Modelo { get; set; }

    /// <summary>IMEI ou número de série.</summary>
    public string? Imei { get; set; }
    public string? SenhaDesbloqueio { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}
