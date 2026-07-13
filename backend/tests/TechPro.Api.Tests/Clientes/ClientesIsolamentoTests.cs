using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Tests.Tenancy;

namespace TechPro.Api.Tests.Clientes;

public class ClientesIsolamentoTests
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
    public void ClientesEAparelhosSaoFiltradosPorTenant()
    {
        var (contexto, provider) = CriarContexto();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        contexto.AddRange(
            new Cliente { TenantId = tenantA, Nome = "Maria de A", Telefone = "11999990001" },
            new Cliente { TenantId = tenantB, Nome = "João de B", Telefone = "11999990002" },
            new Aparelho { TenantId = tenantA, ClienteId = 1, Marca = "Samsung", Modelo = "A54" },
            new Aparelho { TenantId = tenantB, ClienteId = 2, Marca = "Apple", Modelo = "iPhone 13" });
        contexto.SaveChanges();

        provider.TenantId = tenantA;

        Assert.Equal("Maria de A", Assert.Single(contexto.Clientes).Nome);
        Assert.Equal("A54", Assert.Single(contexto.Aparelhos).Modelo);
    }
}
