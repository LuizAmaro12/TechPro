using Microsoft.AspNetCore.Identity;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Usuário interno de uma assistência técnica (dono, gestor, técnico, atendente).
/// Tem tenant_id, mas fica DELIBERADAMENTE fora do Global Query Filter e do RLS:
/// é plano de controle — o login precisa localizar o usuário por e-mail antes de
/// existir contexto de tenant. E-mail é único globalmente no MVP (decisão aprovada).
/// Todo acesso fora do fluxo de autenticação deve validar TenantId explicitamente.
/// </summary>
public class Usuario : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
