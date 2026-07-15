using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.OrdensServico;

/// <summary>
/// Peças utilizadas na OS (módulo 7): baixa automática com custo congelado,
/// devolução com lápide, estoque negativo permitido com aviso e sugestão das
/// peças padrão do serviço.
/// </summary>
public class OsPecasTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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
        return (await resposta.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
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

    private async Task<int> CriarClienteAsync(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria Souza",
            telefone = "(11) 99999-0000",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;
    }

    private async Task<PecaResponse> CriarPecaAsync(
        string token, string nome = "Tela AMOLED", int quantidade = 10,
        decimal custo = 50m, int minimo = 2)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome,
            descricao = (string?)null,
            custoUnitario = custo,
            precoVenda = custo * 2,
            quantidadeEmEstoque = quantidade,
            estoqueMinimo = minimo,
            fornecedorId = (int?)null,
            ativo = true,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PecaResponse>())!;
    }

    private async Task<int> CriarServicoAsync(string token, object[]? pecasPadrao = null)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 300.00,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = pecasPadrao ?? [],
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<OrdemServicoResponse> CriarOsAsync(string token, int clienteId, int servicoId)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = (string?)null,
            aparelhoModelo = (string?)null,
            descricaoProblema = (string?)null,
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task<int> EstoqueAtualAsync(string token, int pecaId)
    {
        var resposta = await EnviarAsync(HttpMethod.Get, $"/api/pecas/{pecaId}", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PecaResponse>())!.QuantidadeEmEstoque;
    }

    [Fact]
    public async Task AdicionarPecaBaixaEstoqueECongelaCusto()
    {
        var token = await RegistrarEmpresaAsync("pecas.baixa@exemplo.com");
        var peca = await CriarPecaAsync(token, quantidade: 10, custo: 50m);
        var os = await CriarOsAsync(token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        var adicionar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId = peca.Id, quantidade = 2 });
        Assert.Equal(HttpStatusCode.Created, adicionar.StatusCode);
        var linha = await adicionar.Content.ReadFromJsonAsync<PecaUsadaResponse>();
        Assert.Equal(50m, linha!.CustoUnitarioNoUso);
        Assert.Equal(8, linha.EstoqueRestante);
        Assert.False(linha.EstoqueAbaixoDoMinimo);
        Assert.Equal(8, await EstoqueAtualAsync(token, peca.Id));

        // Mudar o custo da peça no catálogo não altera o custo congelado.
        var editar = await EnviarAsync(HttpMethod.Put, $"/api/pecas/{peca.Id}", token, new
        {
            nome = "Tela AMOLED",
            descricao = (string?)null,
            custoUnitario = 80m,
            precoVenda = 160m,
            quantidadeEmEstoque = 8,
            estoqueMinimo = 2,
            fornecedorId = (int?)null,
            ativo = true,
        });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(50m, Assert.Single(corpo!.Pecas).CustoUnitarioNoUso);
    }

    [Fact]
    public async Task RemoverPecaDevolveEstoqueEDeixaLapideNoSync()
    {
        var token = await RegistrarEmpresaAsync("pecas.devolucao@exemplo.com");
        var peca = await CriarPecaAsync(token, quantidade: 5);
        var os = await CriarOsAsync(token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        var adicionar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId = peca.Id, quantidade = 3 });
        var linha = await adicionar.Content.ReadFromJsonAsync<PecaUsadaResponse>();
        Assert.Equal(2, await EstoqueAtualAsync(token, peca.Id));

        var remover = await EnviarAsync(
            HttpMethod.Delete, $"/api/ordens-servico/{os.Id}/pecas/{linha!.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, remover.StatusCode);
        Assert.Equal(5, await EstoqueAtualAsync(token, peca.Id));

        var lista = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}/pecas", token);
        Assert.Empty((await lista.Content.ReadFromJsonAsync<List<PecaUsadaResponse>>())!);

        // A lápide viaja no sync (o app offline precisa saber da remoção).
        var sync = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico/sync", token);
        var delta = await sync.Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        var lapide = Assert.Single(delta!.PecasUtilizadas);
        Assert.NotNull(lapide.DeletedAt);
    }

    [Fact]
    public async Task EstoqueNegativoEPermitidoComAviso()
    {
        var token = await RegistrarEmpresaAsync("pecas.negativo@exemplo.com");
        var peca = await CriarPecaAsync(token, quantidade: 1);
        var os = await CriarOsAsync(token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        // Decisão 2026-07-15: a baixa nunca é recusada por falta de saldo.
        var adicionar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId = peca.Id, quantidade = 3 });
        Assert.Equal(HttpStatusCode.Created, adicionar.StatusCode);
        var linha = await adicionar.Content.ReadFromJsonAsync<PecaUsadaResponse>();
        Assert.Equal(-2, linha!.EstoqueRestante);
        Assert.True(linha.EstoqueNegativo);
        Assert.True(linha.EstoqueAbaixoDoMinimo);
    }

    [Fact]
    public async Task AplicarPecasPadraoDoServicoEIdempotente()
    {
        var token = await RegistrarEmpresaAsync("pecas.padrao@exemplo.com");
        var tela = await CriarPecaAsync(token, nome: "Tela", quantidade: 10);
        var cola = await CriarPecaAsync(token, nome: "Cola B7000", quantidade: 20);
        var servicoId = await CriarServicoAsync(token, pecasPadrao:
        [
            new { pecaId = tela.Id, quantidadePadrao = 1 },
            new { pecaId = cola.Id, quantidadePadrao = 2 },
        ]);
        var os = await CriarOsAsync(token, await CriarClienteAsync(token), servicoId);

        var aplicar = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas/aplicar-padrao", token);
        Assert.Equal(HttpStatusCode.OK, aplicar.StatusCode);
        var adicionadas = await aplicar.Content.ReadFromJsonAsync<List<PecaUsadaResponse>>();
        Assert.Equal(2, adicionadas!.Count);
        Assert.Equal(9, await EstoqueAtualAsync(token, tela.Id));
        Assert.Equal(18, await EstoqueAtualAsync(token, cola.Id));

        // Segunda aplicação não duplica nem baixa de novo.
        aplicar = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas/aplicar-padrao", token);
        Assert.Empty((await aplicar.Content.ReadFromJsonAsync<List<PecaUsadaResponse>>())!);
        Assert.Equal(9, await EstoqueAtualAsync(token, tela.Id));
    }

    [Fact]
    public async Task OsFinalizadaNaoRecebeNemDevolvePecas()
    {
        var token = await RegistrarEmpresaAsync("pecas.finalizada@exemplo.com");
        var peca = await CriarPecaAsync(token);
        var os = await CriarOsAsync(token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        var adicionar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId = peca.Id, quantidade = 1 });
        var linha = await adicionar.Content.ReadFromJsonAsync<PecaUsadaResponse>();

        var cancelar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "Cancelado", motivo = "Teste" });
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);

        var depois = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId = peca.Id, quantidade = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, depois.StatusCode);

        var remover = await EnviarAsync(
            HttpMethod.Delete, $"/api/ordens-servico/{os.Id}/pecas/{linha!.Id}", token);
        Assert.Equal(HttpStatusCode.BadRequest, remover.StatusCode);
    }

    [Fact]
    public async Task PecaDeOutraEmpresaNaoEntraNaOs()
    {
        var tokenA = await RegistrarEmpresaAsync("pecas.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("pecas.iso.b@exemplo.com");
        var pecaA = await CriarPecaAsync(tokenA);
        var osB = await CriarOsAsync(tokenB, await CriarClienteAsync(tokenB), await CriarServicoAsync(tokenB));

        // GQF: a peça da A "não existe" para a B → 400.
        var adicionar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osB.Id}/pecas",
            tokenB, new { pecaId = pecaA.Id, quantidade = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, adicionar.StatusCode);

        // E a OS da B "não existe" para a A → 404.
        var cruzado = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osB.Id}/pecas",
            tokenA, new { pecaId = pecaA.Id, quantidade = 1 });
        Assert.Equal(HttpStatusCode.NotFound, cruzado.StatusCode);
    }
}
