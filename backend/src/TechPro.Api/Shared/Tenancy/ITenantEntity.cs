namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Toda entidade que pertence a uma empresa (tenant) implementa esta interface.
/// O TechProDbContext aplica automaticamente um Global Query Filter por
/// <see cref="TenantId"/> a todas elas — nenhuma query de negócio deve
/// filtrar tenant manualmente.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
