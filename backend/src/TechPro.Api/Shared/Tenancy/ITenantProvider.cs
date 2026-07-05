namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Fonte do tenant corrente da requisição. Nulo quando não há usuário
/// autenticado (ex.: login, cadastro) — nesse caso os filtros de tenant
/// são fail-closed: nenhuma linha de tenant é visível.
/// </summary>
public interface ITenantProvider
{
    Guid? TenantId { get; }
}
