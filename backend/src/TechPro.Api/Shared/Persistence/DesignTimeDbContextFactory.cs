using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Persistence;

/// <summary>
/// Usada apenas pelo dotnet-ef (gerar migrations não exige banco de pé).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TechProDbContext>
{
    public TechProDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TECHPRO_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=techpro;Username=techpro_app;Password=techpro_dev";

        var options = new DbContextOptionsBuilder<TechProDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new TechProDbContext(options, new TenantInexistente());
    }

    private sealed class TenantInexistente : ITenantProvider
    {
        public Guid? TenantId => null;
    }
}
