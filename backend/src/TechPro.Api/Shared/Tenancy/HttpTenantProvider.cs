using System.Security.Claims;

namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Resolve o tenant a partir da claim "tenant_id" do JWT da requisição atual.
/// </summary>
public sealed class HttpTenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public Guid? TenantId =>
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id"),
            out var tenantId)
            ? tenantId
            : null;
}
