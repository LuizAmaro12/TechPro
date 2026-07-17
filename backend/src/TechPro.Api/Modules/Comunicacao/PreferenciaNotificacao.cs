using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Preferência de notificação da loja por evento × canal (decisão do usuário
/// 2026-07-16). **Ausência de linha significa ativo** — um tenant novo já nasce
/// com tudo ligado, sem precisar de seed. Só o que a loja desliga vira registro.
/// </summary>
public class PreferenciaNotificacao : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public TipoEventoComunicacao TipoEvento { get; set; }
    public CanalNotificacao Canal { get; set; }
    public bool Ativo { get; set; } = true;
}
