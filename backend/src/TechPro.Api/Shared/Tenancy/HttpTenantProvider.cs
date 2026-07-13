using System.Security.Claims;

namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Resolve o tenant da requisição: primeiro o tenant fixado explicitamente
/// (rota pública por slug, via <see cref="TenantAmbiente"/>), depois a claim
/// "tenant_id" do JWT.
/// </summary>
public sealed class HttpTenantProvider(
    IHttpContextAccessor httpContextAccessor,
    TenantAmbiente ambiente) : ITenantProvider
{
    public Guid? TenantId =>
        ambiente.TenantIdFixado
        ?? (Guid.TryParse(
                httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id"),
                out var tenantId)
            ? tenantId
            : null);
}
