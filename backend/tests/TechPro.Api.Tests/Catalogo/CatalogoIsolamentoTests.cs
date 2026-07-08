using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Tests.Tenancy;

namespace TechPro.Api.Tests.Catalogo;

public class CatalogoIsolamentoTests
{
    private static (TechProDbContext Contexto, TenantProviderFake Provider) CriarContexto()
    {
        var provider = new TenantProviderFake();
        var options = new DbContextOptionsBuilder<TechProDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return (new TechProDbContext(options, provider), provider);
    }

    [Fact]
    public void EntidadesDoCatalogoSaoFiltradasPorTenant()
    {
        var (contexto, provider) = CriarContexto();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        contexto.AddRange(
            new Servico { TenantId = tenantA, Nome = "Troca de tela" },
            new Servico { TenantId = tenantB, Nome = "Troca de bateria" },
            new Peca { TenantId = tenantA, Nome = "Tela iPhone 13" },
            new Peca { TenantId = tenantB, Nome = "Bateria S23" },
            new Fornecedor { TenantId = tenantA, Nome = "PeçaBoa" },
            new Fornecedor { TenantId = tenantB, Nome = "ImportaCel" });
        contexto.SaveChanges();

        provider.TenantId = tenantA;

        Assert.Equal("Troca de tela", Assert.Single(contexto.Servicos).Nome);
        Assert.Equal("Tela iPhone 13", Assert.Single(contexto.Pecas).Nome);
        Assert.Equal("PeçaBoa", Assert.Single(contexto.Fornecedores).Nome);
    }

    [Fact]
    public void SemTenantNoContextoCatalogoFicaVazio()
    {
        var (contexto, provider) = CriarContexto();
        contexto.Add(new Servico { TenantId = Guid.NewGuid(), Nome = "Qualquer" });
        contexto.SaveChanges();

        provider.TenantId = null;

        Assert.Empty(contexto.Servicos.ToList());
    }
}
