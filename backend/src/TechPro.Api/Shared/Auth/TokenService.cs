using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Emite o access token JWT (HS256, 15 min) com as claims de isolamento —
/// tenant_id e role — e gera refresh tokens opacos de 64 bytes aleatórios.
/// </summary>
public class TokenService(IConfiguration configuracao)
{
    public const string ClaimTenantId = "tenant_id";
    public const string ClaimRole = "role";
    public const string ClaimNome = "nome";

    public (string Token, DateTimeOffset ExpiraEm) GerarAccessToken(Usuario usuario, string papel)
    {
        var chave = configuracao["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada.");
        var minutos = configuracao.GetValue("Jwt:AccessTokenMinutos", 15);
        var expiraEm = DateTimeOffset.UtcNow.AddMinutes(minutos);

        var jwt = new JwtSecurityToken(
            issuer: configuracao["Jwt:Issuer"],
            audience: configuracao["Jwt:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimNome, usuario.Nome),
                new Claim(ClaimTenantId, usuario.TenantId.ToString()),
                new Claim(ClaimRole, papel),
            ],
            expires: expiraEm.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiraEm);
    }

    public (string TokenPuro, string TokenHash) GerarRefreshToken()
    {
        var puro = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (puro, HashRefreshToken(puro));
    }

    /// <summary>
    /// SHA-256 simples (sem salt/bcrypt) é suficiente aqui: o token tem 64
    /// bytes aleatórios, então não há dicionário possível — o hash só impede
    /// que um vazamento do banco entregue tokens utilizáveis.
    /// </summary>
    public static string HashRefreshToken(string tokenPuro) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPuro)));
}
