using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Catalogo;

public class CatalogoFluxoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    /// <summary>Registra uma empresa nova e devolve o access token do gestor.</summary>
    private async Task<string> RegistrarEmpresaAsync(string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = $"Loja de {email}",
            nome = "Dono",
            email,
            senha = "senha123",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var auth = await resposta.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    private async Task<HttpResponseMessage> EnviarAsync(
        HttpMethod metodo, string url, string token, object? corpo = null)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return await _cliente.SendAsync(requisicao);
    }

    [Fact]
    public async Task FornecedorCrudCompleto()
    {
        var token = await RegistrarEmpresaAsync("fornecedor.crud@exemplo.com");

        var criado = await EnviarAsync(HttpMethod.Post, "/api/fornecedores", token,
            new { nome = "PeçaBoa Distribuidora", contato = "vendas@pecaboa.com" });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var fornecedor = await criado.Content.ReadFromJsonAsync<FornecedorResponse>();
        Assert.NotNull(fornecedor);

        var lista = await EnviarAsync(HttpMethod.Get, "/api/fornecedores", token);
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var fornecedores = await lista.Content.ReadFromJsonAsync<List<FornecedorResponse>>();
        Assert.Contains(fornecedores!, f => f.Nome == "PeçaBoa Distribuidora");

        var atualizado = await EnviarAsync(HttpMethod.Put, $"/api/fornecedores/{fornecedor.Id}", token,
            new { nome = "PeçaBoa Ltda", contato = "(11) 99999-0000" });
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);

        var removido = await EnviarAsync(HttpMethod.Delete, $"/api/fornecedores/{fornecedor.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
    }

    [Fact]
    public async Task FornecedorDeOutraEmpresaEInvisivel()
    {
        var tokenA = await RegistrarEmpresaAsync("fornecedor.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("fornecedor.iso.b@exemplo.com");

        var criado = await EnviarAsync(HttpMethod.Post, "/api/fornecedores", tokenA,
            new { nome = "Só da Empresa A", contato = (string?)null });
        var fornecedor = await criado.Content.ReadFromJsonAsync<FornecedorResponse>();

        var listaB = await EnviarAsync(HttpMethod.Get, "/api/fornecedores", tokenB);
        var fornecedoresB = await listaB.Content.ReadFromJsonAsync<List<FornecedorResponse>>();
        Assert.DoesNotContain(fornecedoresB!, f => f.Nome == "Só da Empresa A");

        var alterarB = await EnviarAsync(HttpMethod.Put, $"/api/fornecedores/{fornecedor!.Id}", tokenB,
            new { nome = "Tentativa de roubo", contato = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, alterarB.StatusCode);
    }
}
