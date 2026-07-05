using Microsoft.EntityFrameworkCore;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Tests.Tenancy;

// Entidade de teste em escopo de namespace: o EF Core não suporta tipos aninhados.
internal sealed class RecursoTenantTeste : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Nome { get; set; } = "";
}

internal sealed class TenantProviderFake : ITenantProvider
{
    public Guid? TenantId { get; set; }
}

internal sealed class TestDbContext(DbContextOptions options, ITenantProvider tenantProvider)
    : TechProDbContext(options, tenantProvider)
{
    public DbSet<RecursoTenantTeste> Recursos => Set<RecursoTenantTeste>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Registra a entidade de teste ANTES das convenções do contexto base,
        // para que a convenção ITenantEntity aplique o filtro por tenant nela.
        builder.Entity<RecursoTenantTeste>();
        base.OnModelCreating(builder);
    }
}

public class GlobalQueryFilterTests
{
    private static (TestDbContext Contexto, TenantProviderFake Provider) CriarContexto()
    {
        var provider = new TenantProviderFake();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return (new TestDbContext(options, provider), provider);
    }

    [Fact]
    public void EmpresaDeOutroTenantNaoEVisivel()
    {
        var (contexto, provider) = CriarContexto();
        var empresaA = new Empresa { Id = Guid.NewGuid(), Nome = "Assistência A", CriadoEm = DateTimeOffset.UtcNow };
        var empresaB = new Empresa { Id = Guid.NewGuid(), Nome = "Assistência B", CriadoEm = DateTimeOffset.UtcNow };
        contexto.AddRange(empresaA, empresaB);
        contexto.SaveChanges();

        provider.TenantId = empresaA.Id;

        var visiveis = contexto.Empresas.ToList();

        Assert.Single(visiveis);
        Assert.Equal(empresaA.Id, visiveis[0].Id);
    }

    [Fact]
    public void SemTenantNoContextoNenhumaEmpresaEVisivel()
    {
        var (contexto, provider) = CriarContexto();
        contexto.Add(new Empresa { Id = Guid.NewGuid(), Nome = "Assistência A", CriadoEm = DateTimeOffset.UtcNow });
        contexto.SaveChanges();

        provider.TenantId = null;

        Assert.Empty(contexto.Empresas.ToList());
    }

    [Fact]
    public void EntidadeTenantRecebeFiltroAutomaticoPelaConvencao()
    {
        var (contexto, provider) = CriarContexto();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        contexto.AddRange(
            new RecursoTenantTeste { Id = Guid.NewGuid(), TenantId = tenantA, Nome = "de A" },
            new RecursoTenantTeste { Id = Guid.NewGuid(), TenantId = tenantB, Nome = "de B" });
        contexto.SaveChanges();

        provider.TenantId = tenantA;

        var visiveis = contexto.Recursos.ToList();

        Assert.Single(visiveis);
        Assert.Equal("de A", visiveis[0].Nome);
    }

    [Fact]
    public void SemTenantNoContextoEntidadeTenantFicaInvisivel()
    {
        var (contexto, provider) = CriarContexto();
        contexto.Add(new RecursoTenantTeste { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Nome = "qualquer" });
        contexto.SaveChanges();

        provider.TenantId = null;

        Assert.Empty(contexto.Recursos.ToList());
    }

    [Fact]
    public void IgnoreQueryFiltersRevelaTodasAsLinhas()
    {
        var (contexto, provider) = CriarContexto();
        contexto.AddRange(
            new Empresa { Id = Guid.NewGuid(), Nome = "A", CriadoEm = DateTimeOffset.UtcNow },
            new Empresa { Id = Guid.NewGuid(), Nome = "B", CriadoEm = DateTimeOffset.UtcNow });
        contexto.SaveChanges();

        provider.TenantId = null;

        Assert.Equal(2, contexto.Empresas.IgnoreQueryFilters().Count());
    }
}
