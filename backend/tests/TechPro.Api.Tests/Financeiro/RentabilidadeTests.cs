using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Financeiro;

/// <summary>
/// Margem realizada (Fase 2): OS entregues no período, com o custo de peça
/// congelado no momento do uso.
/// </summary>
public class RentabilidadeTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<int> CriarClienteAsync(string token) =>
        (await (await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Cliente Margem",
            telefone = "(11) 98888-7777",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        })).Content.ReadFromJsonAsync<ClienteResponse>())!.Id;

    private async Task<int> CriarServicoAsync(string token, string nome) =>
        (await (await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome,
            categoria = "Reparo",
            precoBase = 300m,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        })).Content.ReadFromJsonAsync<ServicoResponse>())!.Id;

    private async Task<int> CriarPecaAsync(string token, string nome, decimal custo, decimal venda) =>
        (await (await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome,
            descricao = (string?)null,
            custoUnitario = custo,
            precoVenda = venda,
            quantidadeEmEstoque = 50,
            estoqueMinimo = 1,
            fornecedorId = (int?)null,
            ativo = true,
        })).Content.ReadFromJsonAsync<PecaResponse>())!.Id;

    private async Task<OrdemServicoResponse> CriarOsAsync(string token, int clienteId, int servicoId)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
            descricaoProblema = "x",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task MoverAsync(string token, Guid osId, string etapa) =>
        Assert.Equal(HttpStatusCode.OK,
            (await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/etapa",
                token, new { paraEtapa = etapa, motivo = (string?)null })).StatusCode);

    private async Task OrcarAsync(string token, Guid osId, decimal maoDeObra, decimal desconto = 0m)
    {
        await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{osId}/orcamento",
            token, new { valorMaoDeObra = maoDeObra, desconto });
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/orcamento/enviar", token);
    }

    private async Task<RentabilidadeResponse> RentabilidadeAsync(string token, string query = "")
    {
        var resposta = await EnviarAsync(HttpMethod.Get, $"/api/financeiro/rentabilidade{query}", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<RentabilidadeResponse>())!;
    }

    [Fact]
    public async Task MargemUsaOCustoCongeladoDaPecaNaEntrega()
    {
        var token = await RegistrarEmpresaAsync("rent.margem@exemplo.com");
        var pecaId = await CriarPecaAsync(token, "Tela", custo: 100m, venda: 200m);
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token), await CriarServicoAsync(token, "Troca de tela"));

        // 1 peça: custo 100, venda 200. Mão de obra 150 → receita 350.
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId, quantidade = 1 });
        await OrcarAsync(token, os.Id, maoDeObra: 150m);
        await MoverAsync(token, os.Id, "Entregue");

        // O catálogo encarece DEPOIS da entrega — a margem não pode mudar.
        await EnviarAsync(HttpMethod.Put, $"/api/pecas/{pecaId}", token, new
        {
            nome = "Tela",
            descricao = (string?)null,
            custoUnitario = 999m,
            precoVenda = 1500m,
            quantidadeEmEstoque = 49,
            estoqueMinimo = 1,
            fornecedorId = (int?)null,
            ativo = true,
        });

        var rent = await RentabilidadeAsync(token);
        Assert.Equal(1, rent.QuantidadeOs);
        Assert.Equal(0, rent.OsSemOrcamento);
        Assert.Equal(350m, rent.ReceitaTotal);   // 150 mão de obra + 200 peça
        Assert.Equal(100m, rent.CustoPecas);     // custo congelado, não 999
        Assert.Equal(250m, rent.LucroBruto);
        Assert.Equal(71.4m, rent.MargemPercentual); // 250/350
    }

    [Fact]
    public async Task AgrupaPorServicoEOrdenaPeloMaisLucrativo()
    {
        var token = await RegistrarEmpresaAsync("rent.servicos@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var barato = await CriarServicoAsync(token, "Limpeza");
        var lucrativo = await CriarServicoAsync(token, "Troca de tela");

        var os1 = await CriarOsAsync(token, clienteId, barato);
        await OrcarAsync(token, os1.Id, maoDeObra: 80m);
        await MoverAsync(token, os1.Id, "Entregue");

        var os2 = await CriarOsAsync(token, clienteId, lucrativo);
        await OrcarAsync(token, os2.Id, maoDeObra: 400m);
        await MoverAsync(token, os2.Id, "Entregue");

        var rent = await RentabilidadeAsync(token);
        Assert.Equal(2, rent.QuantidadeOs);
        Assert.Equal(480m, rent.ReceitaTotal);
        // Mais lucrativo primeiro.
        Assert.Equal("Troca de tela", rent.PorServico[0].ServicoNome);
        Assert.Equal(400m, rent.PorServico[0].LucroBruto);
        Assert.Equal("Limpeza", rent.PorServico[1].ServicoNome);
        Assert.Equal(80m, rent.PorServico[1].LucroBruto);
    }

    [Fact]
    public async Task SoContaOsEntregueEDentroDoPeriodo()
    {
        var token = await RegistrarEmpresaAsync("rent.entrega@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servicoId = await CriarServicoAsync(token, "Troca de tela");

        // Entregue → conta.
        var entregue = await CriarOsAsync(token, clienteId, servicoId);
        await OrcarAsync(token, entregue.Id, maoDeObra: 200m);
        await MoverAsync(token, entregue.Id, "Entregue");

        // Em reparo (aprovada, mas não entregue) → não conta.
        var emAndamento = await CriarOsAsync(token, clienteId, servicoId);
        await OrcarAsync(token, emAndamento.Id, maoDeObra: 999m);
        await MoverAsync(token, emAndamento.Id, "EmReparo");

        var rent = await RentabilidadeAsync(token);
        Assert.Equal(1, rent.QuantidadeOs);
        Assert.Equal(200m, rent.ReceitaTotal);

        // Período que não contém hoje → zerado.
        var passado = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-30);
        var fora = await RentabilidadeAsync(token,
            $"?de={passado:yyyy-MM-dd}&ate={passado.AddDays(5):yyyy-MM-dd}");
        Assert.Equal(0, fora.QuantidadeOs);
        Assert.Empty(fora.PorServico);

        // Voltar atrás na etapa tira a OS da margem realizada.
        await MoverAsync(token, entregue.Id, "EmTeste");
        Assert.Equal(0, (await RentabilidadeAsync(token)).QuantidadeOs);
    }

    [Fact]
    public async Task OsEntregueSemOrcamentoEContabilizadaESinalizada()
    {
        var token = await RegistrarEmpresaAsync("rent.semorcamento@exemplo.com");
        var pecaId = await CriarPecaAsync(token, "Cola", custo: 20m, venda: 50m);
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token), await CriarServicoAsync(token, "Colagem"));
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId, quantidade = 1 });
        await MoverAsync(token, os.Id, "Entregue"); // sem orçamento

        var rent = await RentabilidadeAsync(token);
        Assert.Equal(1, rent.QuantidadeOs);
        Assert.Equal(1, rent.OsSemOrcamento); // sinalizado para o número ser explicável
        Assert.Equal(0m, rent.ReceitaTotal);
        Assert.Equal(20m, rent.CustoPecas);   // o custo é real
        Assert.Equal(-20m, rent.LucroBruto);
        Assert.Equal(0m, rent.MargemPercentual); // sem receita, não há percentual
    }

    [Fact]
    public async Task RentabilidadeIsolaPorEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("rent.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("rent.iso.b@exemplo.com");
        var os = await CriarOsAsync(
            tokenA, await CriarClienteAsync(tokenA), await CriarServicoAsync(tokenA, "Troca"));
        await OrcarAsync(tokenA, os.Id, maoDeObra: 500m);
        await MoverAsync(tokenA, os.Id, "Entregue");

        var rentB = await RentabilidadeAsync(tokenB);
        Assert.Equal(0, rentB.QuantidadeOs);
        Assert.Equal(0m, rentB.ReceitaTotal);

        Assert.Equal(500m, (await RentabilidadeAsync(tokenA)).ReceitaTotal);
    }
}
