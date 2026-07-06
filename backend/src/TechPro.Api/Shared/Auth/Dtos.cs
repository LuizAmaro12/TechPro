namespace TechPro.Api.Shared.Auth;

// Contratos da API de auth. O refresh token NUNCA aparece aqui: ele viaja
// exclusivamente no cookie httpOnly `techpro_refresh` (decisão aprovada #4).

public record RegistrarRequest(string NomeEmpresa, string Nome, string Email, string Senha);

public record LoginRequest(string Email, string Senha);

public record UsuarioResponse(Guid Id, string Nome, string Email, string Papel, Guid TenantId);

public record AuthResponse(string AccessToken, DateTimeOffset ExpiraEm, UsuarioResponse Usuario);

public record EmpresaResponse(Guid Id, string Nome);

public record MeResponse(
    Guid Id,
    string Nome,
    string Email,
    string Papel,
    Guid TenantId,
    EmpresaResponse Empresa);
