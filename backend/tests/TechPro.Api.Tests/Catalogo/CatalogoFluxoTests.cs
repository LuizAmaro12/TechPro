using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;
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

    private static object CorpoPeca(
        string nome = "Tela iPhone 13", int? fornecedorId = null, int quantidade = 5, int minimo = 2) => new
    {
        nome,
        descricao = "OLED compatível",
        custoUnitario = 450.00m,
        precoVenda = 900.00m,
        quantidadeEmEstoque = quantidade,
        estoqueMinimo = minimo,
        fornecedorId,
        ativo = true,
    };

    [Fact]
    public async Task PecaCrudCompletoComFornecedorEEstoqueBaixo()
    {
        var token = await RegistrarEmpresaAsync("peca.crud@exemplo.com");

        var fornecedorResposta = await EnviarAsync(HttpMethod.Post, "/api/fornecedores", token,
            new { nome = "ImportaCel", contato = (string?)null });
        var fornecedor = await fornecedorResposta.Content.ReadFromJsonAsync<FornecedorResponse>();

        var criada = await EnviarAsync(HttpMethod.Post, "/api/pecas", token,
            CorpoPeca(fornecedorId: fornecedor!.Id));
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var peca = await criada.Content.ReadFromJsonAsync<PecaResponse>();
        Assert.NotNull(peca);
        Assert.Equal("ImportaCel", peca.Fornecedor?.Nome);
        Assert.False(peca.EstoqueBaixo);

        // Fornecedor em uso não pode ser removido (módulo 7 depende do vínculo).
        var removerFornecedor = await EnviarAsync(
            HttpMethod.Delete, $"/api/fornecedores/{fornecedor.Id}", token);
        Assert.Equal(HttpStatusCode.Conflict, removerFornecedor.StatusCode);

        // Quantidade cai para o mínimo: alerta de estoque baixo (módulo 7).
        var atualizada = await EnviarAsync(HttpMethod.Put, $"/api/pecas/{peca.Id}", token,
            CorpoPeca(fornecedorId: fornecedor.Id, quantidade: 2, minimo: 2));
        Assert.Equal(HttpStatusCode.OK, atualizada.StatusCode);
        var pecaAtualizada = await atualizada.Content.ReadFromJsonAsync<PecaResponse>();
        Assert.True(pecaAtualizada!.EstoqueBaixo);

        // Desativar preserva o registro, mas some da listagem padrão.
        var desativada = await EnviarAsync(HttpMethod.Delete, $"/api/pecas/{peca.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, desativada.StatusCode);

        var listaPadrao = await EnviarAsync(HttpMethod.Get, "/api/pecas", token);
        var paginaPadrao = await listaPadrao.Content.ReadFromJsonAsync<PaginaResponse<PecaResponse>>();
        Assert.DoesNotContain(paginaPadrao!.Itens, p => p.Id == peca.Id);

        var listaCompleta = await EnviarAsync(HttpMethod.Get, "/api/pecas?incluirInativas=true", token);
        var paginaCompleta = await listaCompleta.Content.ReadFromJsonAsync<PaginaResponse<PecaResponse>>();
        Assert.Contains(paginaCompleta!.Itens, p => p.Id == peca.Id && !p.Ativo);
    }

    [Fact]
    public async Task PecaDeOutraEmpresaEInvisivel()
    {
        var tokenA = await RegistrarEmpresaAsync("peca.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("peca.iso.b@exemplo.com");

        var criada = await EnviarAsync(HttpMethod.Post, "/api/pecas", tokenA,
            CorpoPeca(nome: "Peça secreta de A"));
        var peca = await criada.Content.ReadFromJsonAsync<PecaResponse>();

        var listaB = await EnviarAsync(HttpMethod.Get, "/api/pecas?incluirInativas=true", tokenB);
        var paginaB = await listaB.Content.ReadFromJsonAsync<PaginaResponse<PecaResponse>>();
        Assert.DoesNotContain(paginaB!.Itens, p => p.Nome == "Peça secreta de A");

        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Get, $"/api/pecas/{peca!.Id}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Delete, $"/api/pecas/{peca.Id}", tokenB)).StatusCode);
    }

    [Fact]
    public async Task PecaComPrecoNegativoRetorna400()
    {
        var token = await RegistrarEmpresaAsync("peca.validacao@exemplo.com");

        var resposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome = "Peça inválida",
            custoUnitario = -1.00m,
            precoVenda = 10.00m,
            quantidadeEmEstoque = 0,
            estoqueMinimo = 0,
            ativo = true,
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CatalogoExigeAutenticacao()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync("/api/pecas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync("/api/fornecedores")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync("/api/servicos")).StatusCode);
    }

    private static object CorpoServico(string nome = "Troca de tela", object[]? pecas = null) => new
    {
        nome,
        categoria = "Tela",
        precoBase = 350.00m,
        duracaoEstimadaMinutos = 60,
        prazoMedioDias = 1,
        exigeDiagnostico = false,
        agendavelOnline = true,
        capacidadeSimultanea = 2,
        ativo = true,
        checklist = new[] { "Testar touch em toda a tela", "Conferir Face ID" },
        pecas = pecas ?? [],
    };

    [Fact]
    public async Task ServicoCompletoComChecklistEPecas()
    {
        var token = await RegistrarEmpresaAsync("servico.crud@exemplo.com");

        var pecaResposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, CorpoPeca(nome: "Tela A54"));
        var peca = await pecaResposta.Content.ReadFromJsonAsync<PecaResponse>();

        var criado = await EnviarAsync(HttpMethod.Post, "/api/servicos", token,
            CorpoServico(pecas: [new { pecaId = peca!.Id, quantidadePadrao = 1 }]));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var servico = await criado.Content.ReadFromJsonAsync<ServicoResponse>();
        Assert.NotNull(servico);
        Assert.Equal(new[] { "Testar touch em toda a tela", "Conferir Face ID" }, servico.Checklist);
        Assert.Equal("Tela A54", Assert.Single(servico.Pecas).Nome);
        Assert.Equal(2, servico.CapacidadeSimultanea);

        // PUT substitui checklist e peças por inteiro.
        var atualizado = await EnviarAsync(HttpMethod.Put, $"/api/servicos/{servico.Id}", token, new
        {
            nome = "Troca de tela premium",
            categoria = "Tela",
            precoBase = 420.00m,
            duracaoEstimadaMinutos = 90,
            prazoMedioDias = 2,
            exigeDiagnostico = true,
            agendavelOnline = true,
            capacidadeSimultanea = 1,
            ativo = true,
            checklist = new[] { "Teste completo pós-reparo" },
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);
        var servicoAtualizado = await atualizado.Content.ReadFromJsonAsync<ServicoResponse>();
        Assert.Equal(new[] { "Teste completo pós-reparo" }, servicoAtualizado!.Checklist);
        Assert.Empty(servicoAtualizado.Pecas);

        // Desativar preserva o registro fora da listagem padrão.
        Assert.Equal(HttpStatusCode.NoContent,
            (await EnviarAsync(HttpMethod.Delete, $"/api/servicos/{servico.Id}", token)).StatusCode);
        var lista = await EnviarAsync(HttpMethod.Get, "/api/servicos", token);
        var pagina = await lista.Content.ReadFromJsonAsync<PaginaResponse<ServicoResponse>>();
        Assert.DoesNotContain(pagina!.Itens, s => s.Id == servico.Id);
    }

    [Fact]
    public async Task ServicoDeOutraEmpresaEInvisivel()
    {
        var tokenA = await RegistrarEmpresaAsync("servico.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("servico.iso.b@exemplo.com");

        var criado = await EnviarAsync(HttpMethod.Post, "/api/servicos", tokenA, CorpoServico(nome: "Só de A"));
        var servico = await criado.Content.ReadFromJsonAsync<ServicoResponse>();

        var listaB = await EnviarAsync(HttpMethod.Get, "/api/servicos?incluirInativos=true", tokenB);
        var paginaB = await listaB.Content.ReadFromJsonAsync<PaginaResponse<ServicoResponse>>();
        Assert.DoesNotContain(paginaB!.Itens, s => s.Nome == "Só de A");

        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Get, $"/api/servicos/{servico!.Id}", tokenB)).StatusCode);
    }

    [Fact]
    public async Task ServicoNaoAceitaPecaDeOutraEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("servico.idor.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("servico.idor.b@exemplo.com");

        var pecaDeA = await (await EnviarAsync(HttpMethod.Post, "/api/pecas", tokenA, CorpoPeca()))
            .Content.ReadFromJsonAsync<PecaResponse>();

        // B tenta referenciar a peça de A pelo id: o GQF a torna inexistente → 400.
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", tokenB,
            CorpoServico(pecas: [new { pecaId = pecaDeA!.Id, quantidadePadrao = 1 }]));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ServicoSemNomeRetorna400()
    {
        var token = await RegistrarEmpresaAsync("servico.validacao@exemplo.com");
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, CorpoServico(nome: ""));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
