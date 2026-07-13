namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// Permite fixar o tenant da requisição fora do JWT — usado pela rota pública
/// de agendamento, que resolve o tenant pelo slug da loja na URL. Uma vez
/// fixado, Global Query Filter e RLS valem normalmente para o restante da
/// requisição, sem reimplementar isolamento à mão no fluxo público.
/// </summary>
public sealed class TenantAmbiente
{
    public Guid? TenantIdFixado { get; set; }
}
