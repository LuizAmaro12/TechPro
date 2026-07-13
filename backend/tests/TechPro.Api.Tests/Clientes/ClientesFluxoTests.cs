using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Clientes;

public class ClientesFluxoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

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

    private static object CorpoCliente(
        string nome = "Maria Souza",
        string telefone = "11999990000",
        bool vip = false,
        int? clientePrincipalId = null,
        bool consentiu = true) => new
    {
        nome,
        telefone,
        email = "maria.souza@exemplo.com",
        cpf = "529.982.247-25",
        endereco = "Rua das Flores, 123 - São Paulo/SP",
        observacoes = "Cliente indicada pela loja vizinha.",
        vip,
        ativo = true,
        clientePrincipalId,
        consentiuComunicacoes = consentiu,
    };

    private async Task<ClienteResponse> CriarClienteAsync(string token, object corpo)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, corpo);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ClienteResponse>())!;
    }

    [Fact]
    public async Task ClienteCrudCompletoComConsentimento()
    {
        var token = await RegistrarEmpresaAsync("cliente.crud@exemplo.com");

        var criado = await CriarClienteAsync(token, CorpoCliente());
        Assert.True(criado.ConsentiuComunicacoes);
        Assert.NotNull(criado.ConsentimentoEm);

        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/clientes/{criado.Id}", token);
        Assert.Equal(HttpStatusCode.OK, detalhe.StatusCode);
        var cliente = await detalhe.Content.ReadFromJsonAsync<ClienteDetalheResponse>();
        Assert.Equal("Maria Souza", cliente!.Nome);
        Assert.Empty(cliente.Aparelhos);

        var atualizado = await EnviarAsync(HttpMethod.Put, $"/api/clientes/{criado.Id}", token,
            CorpoCliente(nome: "Maria Souza Lima", vip: true));
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);

        var desativado = await EnviarAsync(HttpMethod.Delete, $"/api/clientes/{criado.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, desativado.StatusCode);

        var listaPadrao = await EnviarAsync(HttpMethod.Get, "/api/clientes", token);
        var paginaPadrao = await listaPadrao.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        Assert.DoesNotContain(paginaPadrao!.Itens, c => c.Id == criado.Id);

        var listaCompleta = await EnviarAsync(HttpMethod.Get, "/api/clientes?incluirInativos=true", token);
        var paginaCompleta = await listaCompleta.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        Assert.Contains(paginaCompleta!.Itens, c => c.Id == criado.Id && !c.Ativo);
    }

    [Fact]
    public async Task FiltrosVipEBuscaFuncionam()
    {
        var token = await RegistrarEmpresaAsync("cliente.filtros@exemplo.com");
        await CriarClienteAsync(token, CorpoCliente(nome: "Ana Comum", telefone: "11988880001"));
        await CriarClienteAsync(token, CorpoCliente(nome: "Bruno Vip", telefone: "11988880002", vip: true));

        var somenteVip = await EnviarAsync(HttpMethod.Get, "/api/clientes?somenteVip=true", token);
        var paginaVip = await somenteVip.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        Assert.Equal("Bruno Vip", Assert.Single(paginaVip!.Itens).Nome);

        var busca = await EnviarAsync(HttpMethod.Get, "/api/clientes?busca=88880001", token);
        var paginaBusca = await busca.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        Assert.Equal("Ana Comum", Assert.Single(paginaBusca!.Itens).Nome);
    }

    [Fact]
    public async Task ClienteDeOutraEmpresaEInvisivel()
    {
        var tokenA = await RegistrarEmpresaAsync("cliente.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("cliente.iso.b@exemplo.com");

        var deA = await CriarClienteAsync(tokenA, CorpoCliente(nome: "Sigiloso de A"));

        var listaB = await EnviarAsync(HttpMethod.Get, "/api/clientes?incluirInativos=true", tokenB);
        var paginaB = await listaB.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        Assert.DoesNotContain(paginaB!.Itens, c => c.Nome == "Sigiloso de A");

        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Get, $"/api/clientes/{deA.Id}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Put, $"/api/clientes/{deA.Id}", tokenB, CorpoCliente())).StatusCode);
    }

    [Fact]
    public async Task VinculoRespeitaAsRegrasDeUmNivel()
    {
        var token = await RegistrarEmpresaAsync("cliente.vinculo@exemplo.com");
        var principal = await CriarClienteAsync(token, CorpoCliente(nome: "Pai da Família", telefone: "11977770001"));
        var vinculado = await CriarClienteAsync(token,
            CorpoCliente(nome: "Filho da Família", telefone: "11977770002", clientePrincipalId: principal.Id));
        Assert.Equal(principal.Id, vinculado.ClientePrincipal?.Id);

        // 2 níveis: vincular alguém a quem já é vinculado → 400.
        var doisNiveis = await EnviarAsync(HttpMethod.Post, "/api/clientes", token,
            CorpoCliente(nome: "Neto", telefone: "11977770003", clientePrincipalId: vinculado.Id));
        Assert.Equal(HttpStatusCode.BadRequest, doisNiveis.StatusCode);

        // Auto-vínculo → 400.
        var autoVinculo = await EnviarAsync(HttpMethod.Put, $"/api/clientes/{principal.Id}", token,
            CorpoCliente(nome: "Pai da Família", telefone: "11977770001", clientePrincipalId: principal.Id));
        Assert.Equal(HttpStatusCode.BadRequest, autoVinculo.StatusCode);

        // Principal com dependentes não pode virar vinculado.
        var outro = await CriarClienteAsync(token, CorpoCliente(nome: "Avulso", telefone: "11977770004"));
        var principalVirandoVinculado = await EnviarAsync(HttpMethod.Put, $"/api/clientes/{principal.Id}", token,
            CorpoCliente(nome: "Pai da Família", telefone: "11977770001", clientePrincipalId: outro.Id));
        Assert.Equal(HttpStatusCode.BadRequest, principalVirandoVinculado.StatusCode);
    }

    [Fact]
    public async Task VinculoNaoAceitaClienteDeOutraEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("cliente.vinculo.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("cliente.vinculo.b@exemplo.com");
        var deA = await CriarClienteAsync(tokenA, CorpoCliente(nome: "Principal de A"));

        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", tokenB,
            CorpoCliente(nome: "Invasor", telefone: "11966660001", clientePrincipalId: deA.Id));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ValidacaoRejeitaDadosInvalidos()
    {
        var token = await RegistrarEmpresaAsync("cliente.validacao@exemplo.com");

        var semNome = await EnviarAsync(HttpMethod.Post, "/api/clientes", token,
            new { nome = "", telefone = "11999990000", consentiuComunicacoes = false, ativo = true, vip = false });
        Assert.Equal(HttpStatusCode.BadRequest, semNome.StatusCode);

        var cpfInvalido = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "CPF Errado",
            telefone = "11999990000",
            cpf = "123",
            consentiuComunicacoes = false,
            ativo = true,
            vip = false,
        });
        Assert.Equal(HttpStatusCode.BadRequest, cpfInvalido.StatusCode);
    }

    [Fact]
    public async Task ClientesExigemAutenticacao()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync("/api/clientes")).StatusCode);
    }

    private static object CorpoAparelho(string modelo = "Galaxy A54") => new
    {
        marca = "Samsung",
        modelo,
        imei = "356789104563218",
        senhaDesbloqueio = "1234",
        observacoes = "Tela trincada no canto.",
        ativo = true,
    };

    [Fact]
    public async Task AparelhosDoClienteCrudCompleto()
    {
        var token = await RegistrarEmpresaAsync("aparelho.crud@exemplo.com");
        var cliente = await CriarClienteAsync(token, CorpoCliente(nome: "Dona dos Aparelhos", telefone: "11955550001"));

        var criado = await EnviarAsync(HttpMethod.Post, $"/api/clientes/{cliente.Id}/aparelhos", token, CorpoAparelho());
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var aparelho = await criado.Content.ReadFromJsonAsync<AparelhoResponse>();
        Assert.Equal("Galaxy A54", aparelho!.Modelo);

        var atualizado = await EnviarAsync(HttpMethod.Put,
            $"/api/clientes/{cliente.Id}/aparelhos/{aparelho.Id}", token, CorpoAparelho(modelo: "Galaxy A54 5G"));
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);

        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/clientes/{cliente.Id}", token);
        var clienteDetalhe = await detalhe.Content.ReadFromJsonAsync<ClienteDetalheResponse>();
        Assert.Equal("Galaxy A54 5G", Assert.Single(clienteDetalhe!.Aparelhos).Modelo);

        var desativado = await EnviarAsync(HttpMethod.Delete,
            $"/api/clientes/{cliente.Id}/aparelhos/{aparelho.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, desativado.StatusCode);

        var detalheDepois = await EnviarAsync(HttpMethod.Get, $"/api/clientes/{cliente.Id}", token);
        var clienteDepois = await detalheDepois.Content.ReadFromJsonAsync<ClienteDetalheResponse>();
        Assert.DoesNotContain(clienteDepois!.Aparelhos, a => a.Ativo);
    }

    [Fact]
    public async Task AparelhoDeClienteDeOutraEmpresaRetorna404()
    {
        var tokenA = await RegistrarEmpresaAsync("aparelho.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("aparelho.iso.b@exemplo.com");
        var clienteDeA = await CriarClienteAsync(tokenA, CorpoCliente(nome: "Cliente de A"));

        var resposta = await EnviarAsync(HttpMethod.Post,
            $"/api/clientes/{clienteDeA.Id}/aparelhos", tokenB, CorpoAparelho());

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
