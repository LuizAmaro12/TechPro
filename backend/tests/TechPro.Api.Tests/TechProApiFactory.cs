using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Tests;

/// <summary>
/// Fábrica de API para testes de integração de qualquer módulo: Sqlite em
/// memória no lugar do Postgres (sem interceptor de sessão RLS, que é
/// específico de Postgres) e rate limit alto para os testes de auth não
/// esbarrarem no limite de produção.
/// </summary>
public sealed class TechProApiFactory : WebApplicationFactory<Program>
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
