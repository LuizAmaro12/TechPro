namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Origem do refresh token (seção 8 do doc de stack): o app mobile do técnico
/// (Fase 2) terá política de expiração própria, mais longa e tolerante a
/// janelas offline. O campo existe desde já para evitar migração da tabela.
/// </summary>
public enum TipoCliente
{
    Web = 0,
    Mobile = 1,
}

/// <summary>
/// Refresh token persistido com rotação: cada uso revoga o token e emite um
/// sucessor (SubstituidoPorId). Só o hash SHA-256 vai ao banco — o valor puro
/// vive apenas no cookie httpOnly do cliente.
/// Assim como Usuario, é plano de controle: fica fora do GQF/RLS porque o
/// refresh acontece antes de existir JWT válido; o AuthService valida o
/// vínculo usuário/tenant explicitamente.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Guid TenantId { get; set; }
    public required string TokenHash { get; set; }
    public TipoCliente TipoCliente { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? RevogadoEm { get; set; }
    public Guid? SubstituidoPorId { get; set; }

    public bool EstaAtivo => RevogadoEm is null && DateTimeOffset.UtcNow < ExpiraEm;
}
