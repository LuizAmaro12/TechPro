using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Catalogo;

/// <summary>
/// Etapa "estoque com movimentação rastreável" (Fase 2). O ponto central é a
/// **reconciliação**: a soma do razão tem de bater com o saldo da peça, seja
/// qual for o caminho que alterou o estoque.
/// </summary>
public class EstoqueMovimentacaoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<PecaResponse> CriarPecaAsync(
        string token, string nome = "Tela", int estoque = 10, int minimo = 2, int? fornecedorId = null)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome,
            descricao = (string?)null,
            custoUnitario = 100.00,
            precoVenda = 200.00,
            quantidadeEmEstoque = estoque,
            estoqueMinimo = minimo,
            fornecedorId,
            ativo = true,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PecaResponse>())!;
    }

    private async Task<List<MovimentacaoResponse>> ExtratoAsync(string token, int pecaId) =>
        (await (await EnviarAsync(HttpMethod.Get, $"/api/pecas/{pecaId}/movimentacoes", token))
            .Content.ReadFromJsonAsync<List<MovimentacaoResponse>>())!;

    private async Task<PecaResponse> ObterPecaAsync(string token, int pecaId) =>
        (await (await EnviarAsync(HttpMethod.Get, $"/api/pecas/{pecaId}", token))
            .Content.ReadFromJsonAsync<PecaResponse>())!;

    /// <summary>A garantia que dá sentido ao razão inteiro.</summary>
    private static void AssertReconcilia(List<MovimentacaoResponse> extrato, PecaResponse peca)
    {
        Assert.Equal(peca.QuantidadeEmEstoque, extrato.Sum(m => m.Quantidade));
        // SaldoApos do movimento mais recente também tem de bater.
        if (extrato.Count > 0)
        {
            Assert.Equal(peca.QuantidadeEmEstoque, extrato[0].SaldoApos);
        }
    }

    [Fact]
    public async Task EstoqueInicialDoCadastroEntraNoRazao()
    {
        var token = await RegistrarEmpresaAsync($"inicial.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 10);

        var extrato = await ExtratoAsync(token, peca.Id);
        var movimento = Assert.Single(extrato);
        Assert.Equal(TipoMovimentacaoEstoque.Entrada, movimento.Tipo);
        Assert.Equal(10, movimento.Quantidade);
        Assert.Equal(10, movimento.SaldoApos);
        Assert.Equal("Dono", movimento.UsuarioNome);
        AssertReconcilia(extrato, await ObterPecaAsync(token, peca.Id));
    }

    [Fact]
    public async Task PecaCriadaComEstoqueZeroNaoGeraMovimentoVazio()
    {
        var token = await RegistrarEmpresaAsync($"zero.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 0);
        Assert.Empty(await ExtratoAsync(token, peca.Id));
    }

    [Fact]
    public async Task EntradaESaidaManuaisAjustamOSaldoEReconciliam()
    {
        var token = await RegistrarEmpresaAsync($"manual.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 10);

        var entrada = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "Entrada", quantidade = 5, custoUnitario = 120.00, motivo = "Compra do mês" });
        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);

        await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "Saida", quantidade = 3, custoUnitario = (decimal?)null, motivo = "Peça quebrou na bancada" });

        var atualizada = await ObterPecaAsync(token, peca.Id);
        Assert.Equal(12, atualizada.QuantidadeEmEstoque);
        // Entrada com custo informado atualiza o custo da peça (é o preço pago).
        Assert.Equal(120.00m, atualizada.CustoUnitario);
        AssertReconcilia(await ExtratoAsync(token, peca.Id), atualizada);
    }

    /// <summary>
    /// Ajuste recebe o **saldo desejado**, não o delta — é como a loja conta
    /// prateleira. O razão guarda a diferença.
    /// </summary>
    [Fact]
    public async Task AjusteLevaAoSaldoInformadoEExigeMotivo()
    {
        var token = await RegistrarEmpresaAsync($"ajuste.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 10);

        var semMotivo = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "Ajuste", quantidade = 7, custoUnitario = (decimal?)null, motivo = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, semMotivo.StatusCode);

        var ok = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "Ajuste", quantidade = 7, custoUnitario = (decimal?)null, motivo = "Contagem de prateleira" });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var movimento = (await ok.Content.ReadFromJsonAsync<MovimentacaoResponse>())!;
        Assert.Equal(-3, movimento.Quantidade);
        Assert.Equal(7, movimento.SaldoApos);

        var atualizada = await ObterPecaAsync(token, peca.Id);
        Assert.Equal(7, atualizada.QuantidadeEmEstoque);
        AssertReconcilia(await ExtratoAsync(token, peca.Id), atualizada);

        // Ajuste que não muda nada é ruído no extrato.
        var semEfeito = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "Ajuste", quantidade = 7, custoUnitario = (decimal?)null, motivo = "de novo" });
        Assert.Equal(HttpStatusCode.BadRequest, semEfeito.StatusCode);
    }

    /// <summary>
    /// Era o buraco: a edição do catálogo sobrescrevia o saldo em silêncio.
    /// Agora a diferença vira um Ajuste rastreável.
    /// </summary>
    [Fact]
    public async Task EdicaoDaPecaNoCatalogoGeraAjusteRastreavel()
    {
        var token = await RegistrarEmpresaAsync($"edicao.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 10);

        await EnviarAsync(HttpMethod.Put, $"/api/pecas/{peca.Id}", token, new
        {
            nome = "Tela",
            descricao = (string?)null,
            custoUnitario = 100.00,
            precoVenda = 250.00,
            quantidadeEmEstoque = 15,
            estoqueMinimo = 2,
            fornecedorId = (int?)null,
            ativo = true,
        });

        var extrato = await ExtratoAsync(token, peca.Id);
        var ajuste = extrato[0];
        Assert.Equal(TipoMovimentacaoEstoque.Ajuste, ajuste.Tipo);
        Assert.Equal(5, ajuste.Quantidade);
        Assert.Equal(15, ajuste.SaldoApos);
        AssertReconcilia(extrato, await ObterPecaAsync(token, peca.Id));
    }

    [Fact]
    public async Task EdicaoQueNaoMexeNoSaldoNaoPoluiOExtrato()
    {
        var token = await RegistrarEmpresaAsync($"edicaolimpa.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 10);

        await EnviarAsync(HttpMethod.Put, $"/api/pecas/{peca.Id}", token, new
        {
            nome = "Tela premium",
            descricao = "OLED",
            custoUnitario = 100.00,
            precoVenda = 300.00,
            quantidadeEmEstoque = 10,
            estoqueMinimo = 2,
            fornecedorId = (int?)null,
            ativo = true,
        });

        Assert.Single(await ExtratoAsync(token, peca.Id)); // só o estoque inicial
    }

    /// <summary>Consumo e estorno da OS entram no razão amarrados à ordem.</summary>
    [Fact]
    public async Task ConsumoEEstornoDaOsAparecemNoExtratoComANumeroDaOs()
    {
        var token = await RegistrarEmpresaAsync($"os.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 10);

        var cliente = (await (await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria",
            telefone = "(11) 90000-0000",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        })).Content.ReadFromJsonAsync<ClienteResponse>())!;

        var servico = (await (await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = (string?)null,
            precoBase = 300.00,
            duracaoEstimadaMinutos = 60,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            slaHoras = (int?)null,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        })).Content.ReadFromJsonAsync<ServicoResponse>())!;

        var os = (await (await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId = cliente.Id,
            servicoId = servico.Id,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
            descricaoProblema = "Tela trincada",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        })).Content.ReadFromJsonAsync<OrdemServicoResponse>())!;

        var linha = (await (await EnviarAsync(
                HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas", token,
                new { pecaId = peca.Id, quantidade = 4 }))
            .Content.ReadFromJsonAsync<PecaUsadaResponse>())!;

        var aposConsumo = await ExtratoAsync(token, peca.Id);
        Assert.Equal(TipoMovimentacaoEstoque.ConsumoOs, aposConsumo[0].Tipo);
        Assert.Equal(-4, aposConsumo[0].Quantidade);
        Assert.Equal(6, aposConsumo[0].SaldoApos);
        Assert.Equal(os.Numero, aposConsumo[0].OrdemServicoNumero);
        AssertReconcilia(aposConsumo, await ObterPecaAsync(token, peca.Id));

        await EnviarAsync(
            HttpMethod.Delete, $"/api/ordens-servico/{os.Id}/pecas/{linha.Id}", token);

        var aposEstorno = await ExtratoAsync(token, peca.Id);
        Assert.Equal(TipoMovimentacaoEstoque.EstornoOs, aposEstorno[0].Tipo);
        Assert.Equal(4, aposEstorno[0].Quantidade);
        var final = await ObterPecaAsync(token, peca.Id);
        Assert.Equal(10, final.QuantidadeEmEstoque);
        AssertReconcilia(aposEstorno, final);
    }

    /// <summary>Movimento de OS não pode ser forjado pela mão.</summary>
    [Fact]
    public async Task MovimentoDeOsNaoPodeSerCriadoManualmente()
    {
        var token = await RegistrarEmpresaAsync($"forja.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token);

        var resposta = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "ConsumoOs", quantidade = 1, custoUnitario = (decimal?)null, motivo = "tentativa" });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task EstoqueNegativoContinuaPermitidoESaiRegistrado()
    {
        var token = await RegistrarEmpresaAsync($"negativo.{Guid.NewGuid():N}@exemplo.com");
        var peca = await CriarPecaAsync(token, estoque: 2);

        var resposta = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{peca.Id}/movimentacoes", token,
            new { tipo = "Saida", quantidade = 5, custoUnitario = (decimal?)null, motivo = "uso interno" });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var atualizada = await ObterPecaAsync(token, peca.Id);
        Assert.Equal(-3, atualizada.QuantidadeEmEstoque);
        AssertReconcilia(await ExtratoAsync(token, peca.Id), atualizada);
    }

    // --- Lista de compra -------------------------------------------------------------

    [Fact]
    public async Task ListaDeCompraAgrupaPorFornecedorESugereQuantidade()
    {
        var token = await RegistrarEmpresaAsync($"compra.{Guid.NewGuid():N}@exemplo.com");

        var fornecedor = (await (await EnviarAsync(HttpMethod.Post, "/api/fornecedores", token, new
        {
            nome = "Distribuidora Sul",
            contato = "(11) 3333-0000",
        })).Content.ReadFromJsonAsync<FornecedorResponse>())!;

        // Abaixo do mínimo → repor até o mínimo (5 - 1 = 4).
        await CriarPecaAsync(token, "Bateria", estoque: 1, minimo: 5, fornecedorId: fornecedor.Id);
        // No mínimo exato → sugere ao menos 1, senão apareceria sem sugestão.
        await CriarPecaAsync(token, "Conector", estoque: 3, minimo: 3, fornecedorId: fornecedor.Id);
        // Sem fornecedor → não pode sumir da lista.
        await CriarPecaAsync(token, "Película", estoque: 0, minimo: 2);
        // Confortável → fica de fora.
        await CriarPecaAsync(token, "Tela", estoque: 50, minimo: 2, fornecedorId: fornecedor.Id);

        var lista = (await (await EnviarAsync(HttpMethod.Get, "/api/estoque/lista-compra", token))
            .Content.ReadFromJsonAsync<ListaCompraResponse>())!;

        Assert.Equal(3, lista.TotalDeItens);
        Assert.Equal(2, lista.Grupos.Count);

        var grupoFornecedor = lista.Grupos.Single(g => g.FornecedorId == fornecedor.Id);
        var bateria = grupoFornecedor.Itens.Single(i => i.PecaNome == "Bateria");
        Assert.Equal(4, bateria.SugestaoCompra);
        Assert.Equal(400.00m, bateria.CustoEstimado);
        Assert.Equal(1, grupoFornecedor.Itens.Single(i => i.PecaNome == "Conector").SugestaoCompra);

        var semFornecedor = lista.Grupos.Single(g => g.FornecedorId is null);
        Assert.Equal("Sem fornecedor definido", semFornecedor.FornecedorNome);
        Assert.Equal(2, semFornecedor.Itens.Single().SugestaoCompra);

        Assert.Equal(lista.Grupos.Sum(g => g.CustoEstimado), lista.CustoEstimado);
        Assert.DoesNotContain(lista.Grupos.SelectMany(g => g.Itens), i => i.PecaNome == "Tela");
    }

    // --- Isolamento -----------------------------------------------------------------

    [Fact]
    public async Task ExtratoEListaDeCompraNaoAtravessamEmpresas()
    {
        var tokenA = await RegistrarEmpresaAsync($"estA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"estB.{Guid.NewGuid():N}@exemplo.com");

        var pecaDeA = await CriarPecaAsync(tokenA, "Tela da A", estoque: 0, minimo: 5);

        // A peça de A "não existe" para B (GQF + RLS).
        var lendo = await EnviarAsync(
            HttpMethod.Get, $"/api/pecas/{pecaDeA.Id}/movimentacoes", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, lendo.StatusCode);

        var movimentando = await EnviarAsync(
            HttpMethod.Post, $"/api/pecas/{pecaDeA.Id}/movimentacoes", tokenB,
            new { tipo = "Entrada", quantidade = 1, custoUnitario = (decimal?)null, motivo = "invasão" });
        Assert.Equal(HttpStatusCode.NotFound, movimentando.StatusCode);

        var listaDeB = (await (await EnviarAsync(HttpMethod.Get, "/api/estoque/lista-compra", tokenB))
            .Content.ReadFromJsonAsync<ListaCompraResponse>())!;
        Assert.Empty(listaDeB.Grupos);
    }
}
