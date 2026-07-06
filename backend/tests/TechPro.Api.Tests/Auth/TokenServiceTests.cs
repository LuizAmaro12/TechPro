using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Auth;

public class TokenServiceTests
{
    private static TokenService CriarServico() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-suficientemente-longa-para-hs256-64-bytes!!!!",
                ["Jwt:Issuer"] = "TechPro",
                ["Jwt:Audience"] = "TechPro",
                ["Jwt:AccessTokenMinutos"] = "15",
            })
            .Build());

    private static Usuario CriarUsuario() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Nome = "Dona Maria",
        UserName = "maria@exemplo.com",
        Email = "maria@exemplo.com",
    };

    [Fact]
    public void AccessTokenCarregaClaimsDeTenantEPapel()
    {
        var servico = CriarServico();
        var usuario = CriarUsuario();

        var (token, expiraEm) = servico.GerarAccessToken(usuario, Papeis.Gestor);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(usuario.TenantId.ToString(), jwt.Claims.Single(c => c.Type == "tenant_id").Value);
        Assert.Equal("gestor", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal(usuario.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("TechPro", jwt.Issuer);
        Assert.InRange((expiraEm - DateTimeOffset.UtcNow).TotalMinutes, 13, 16);
    }

    [Fact]
    public void RefreshTokenGeraValoresUnicosComHashDeterministico()
    {
        var servico = CriarServico();

        var (puro1, hash1) = servico.GerarRefreshToken();
        var (puro2, hash2) = servico.GerarRefreshToken();

        Assert.NotEqual(puro1, puro2);
        Assert.NotEqual(hash1, hash2);
        Assert.Equal(hash1, TokenService.HashRefreshToken(puro1));
        Assert.NotEqual(puro1, hash1);
    }
}
