using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TechPro.Api.Shared.Auth;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Tests.Auth;

/// <summary>
/// Fábrica de API para testes de integração: Sqlite em memória no lugar do
/// Postgres (sem interceptor de sessão RLS, que é específico de Postgres) e
/// rate limit alto para os testes não esbarrarem no limite de produção.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "chave-de-teste-suficientemente-longa-para-hs256-64-bytes!!!!");
        builder.UseSetting("Jwt:Issuer", "TechPro");
        builder.UseSetting("Jwt:Audience", "TechPro");
        builder.UseSetting("Jwt:AccessTokenMinutos", "15");
        builder.UseSetting("RateLimiting:AuthPorMinuto", "1000");

        builder.ConfigureServices(services =>
        {
            // EF Core 8+: AddDbContext registra a configuração das options como
            // serviço próprio — sem removê-la, Npgsql e Sqlite ficam empilhados.
            services.RemoveAll<IDbContextOptionsConfiguration<TechProDbContext>>();
            services.RemoveAll<DbContextOptions<TechProDbContext>>();
            _conexao.Open();
            services.AddDbContext<TechProDbContext>(o => o.UseSqlite(_conexao));

            using var provisorio = services.BuildServiceProvider();
            using var escopo = provisorio.CreateScope();
            escopo.ServiceProvider.GetRequiredService<TechProDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conexao.Dispose();
    }
}

public class AuthFluxoTests(AuthApiFactory fabrica) : IClassFixture<AuthApiFactory>
{
    private const string NomeCookie = "techpro_refresh";
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private static object CorpoRegistro(string email, string senha = "senha123") => new
    {
        nomeEmpresa = "AssisTech da Maria",
        nome = "Maria Silva",
        email,
        senha,
    };

    private static string ExtrairCookieRefresh(HttpResponseMessage resposta)
    {
        var setCookie = resposta.Headers.GetValues("Set-Cookie")
            .Single(v => v.StartsWith($"{NomeCookie}="));

        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);

        return setCookie.Split(';')[0][$"{NomeCookie}=".Length..];
    }

    private HttpRequestMessage RequisicaoComCookie(HttpMethod metodo, string url, string cookie)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Add("Cookie", $"{NomeCookie}={cookie}");
        return requisicao;
    }

    [Fact]
    public async Task FluxoCompletoRegistrarMeRefreshComRotacaoEDeteccaoDeReuso()
    {
        // Registrar: cria empresa + gestor e devolve JWT com tenant_id.
        var registro = await _cliente.PostAsJsonAsync(
            "/api/auth/registrar", CorpoRegistro("maria.fluxo@exemplo.com"));

        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var auth = await registro.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(Papeis.Gestor, auth.Usuario.Papel);
        Assert.NotEqual(Guid.Empty, auth.Usuario.TenantId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);
        Assert.Equal(auth.Usuario.TenantId.ToString(),
            jwt.Claims.Single(c => c.Type == "tenant_id").Value);
        Assert.Equal(Papeis.Gestor, jwt.Claims.Single(c => c.Type == "role").Value);

        var cookieOriginal = ExtrairCookieRefresh(registro);

        // Me: autorizado pelo Bearer, lê a empresa através do Global Query Filter.
        var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var respostaMe = await _cliente.SendAsync(me);

        Assert.Equal(HttpStatusCode.OK, respostaMe.StatusCode);
        var perfil = await respostaMe.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(perfil);
        Assert.Equal("AssisTech da Maria", perfil.Empresa.Nome);
        Assert.Equal(auth.Usuario.TenantId, perfil.Empresa.Id);

        // Refresh: rotaciona o token — novo cookie, novo access token.
        var refresh = await _cliente.SendAsync(
            RequisicaoComCookie(HttpMethod.Post, "/api/auth/refresh", cookieOriginal));

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var cookieRotacionado = ExtrairCookieRefresh(refresh);
        Assert.NotEqual(cookieOriginal, cookieRotacionado);

        // Reuso do cookie antigo = possível roubo: 401 e derruba a família toda.
        var reuso = await _cliente.SendAsync(
            RequisicaoComCookie(HttpMethod.Post, "/api/auth/refresh", cookieOriginal));
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);

        var aposReuso = await _cliente.SendAsync(
            RequisicaoComCookie(HttpMethod.Post, "/api/auth/refresh", cookieRotacionado));
        Assert.Equal(HttpStatusCode.Unauthorized, aposReuso.StatusCode);
    }

    [Fact]
    public async Task LoginValidaCredenciaisEDevolveTokens()
    {
        await _cliente.PostAsJsonAsync("/api/auth/registrar", CorpoRegistro("maria.login@exemplo.com"));

        var errado = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "maria.login@exemplo.com", senha = "senha-errada1" });
        Assert.Equal(HttpStatusCode.Unauthorized, errado.StatusCode);

        var certo = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "maria.login@exemplo.com", senha = "senha123" });
        Assert.Equal(HttpStatusCode.OK, certo.StatusCode);

        var auth = await certo.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(Papeis.Gestor, auth.Usuario.Papel);
        ExtrairCookieRefresh(certo);
    }

    [Fact]
    public async Task LogoutRevogaRefreshToken()
    {
        var registro = await _cliente.PostAsJsonAsync(
            "/api/auth/registrar", CorpoRegistro("maria.logout@exemplo.com"));
        var cookie = ExtrairCookieRefresh(registro);

        var logout = await _cliente.SendAsync(
            RequisicaoComCookie(HttpMethod.Post, "/api/auth/logout", cookie));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _cliente.SendAsync(
            RequisicaoComCookie(HttpMethod.Post, "/api/auth/refresh", cookie));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task RegistrarComSenhaFracaRetorna400()
    {
        var resposta = await _cliente.PostAsJsonAsync(
            "/api/auth/registrar", CorpoRegistro("maria.senha@exemplo.com", senha: "abc"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task RegistrarComEmailDuplicadoRetorna409()
    {
        await _cliente.PostAsJsonAsync("/api/auth/registrar", CorpoRegistro("maria.dupla@exemplo.com"));
        var repetido = await _cliente.PostAsJsonAsync(
            "/api/auth/registrar", CorpoRegistro("maria.dupla@exemplo.com"));

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task MeSemTokenRetorna401()
    {
        var resposta = await _cliente.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RefreshSemCookieRetorna401()
    {
        var resposta = await _cliente.PostAsync("/api/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CincoSenhasErradasBloqueiamAContaTemporariamente()
    {
        await _cliente.PostAsJsonAsync("/api/auth/registrar", CorpoRegistro("maria.lockout@exemplo.com"));

        for (var i = 0; i < 5; i++)
        {
            await _cliente.PostAsJsonAsync("/api/auth/login",
                new { email = "maria.lockout@exemplo.com", senha = "senha-errada1" });
        }

        // Mesmo com a senha certa, a conta está em lockout (5 falhas / 5 min).
        var bloqueado = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "maria.lockout@exemplo.com", senha = "senha123" });
        Assert.Equal(HttpStatusCode.Unauthorized, bloqueado.StatusCode);
    }
}
