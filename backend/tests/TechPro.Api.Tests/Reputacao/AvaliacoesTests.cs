using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.Reputacao.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Reputacao;

/// <summary>
/// Etapa "avaliações e reputação" (Fase 2): estrelas + NPS, gatilho só após
/// entrega, resumo por técnico e fechamento de loop de avaliação negativa.
/// </summary>
public class AvaliacoesTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private async Task<(string Token, string Slug)> RegistrarLojaAsync(string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = $"Loja de {email}",
            nome = "Dono",
            email,
            senha = "senha123",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var token = (await resposta.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        var loja = await (await EnviarAsync(HttpMethod.Get, "/api/configuracoes/loja", token))
            .Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        return (token, loja!.RootElement.GetProperty("slug").GetString()!);
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

    private async Task<Guid> GuidDoDonoAsync(string token)
    {
        var equipe = await (await EnviarAsync(HttpMethod.Get, "/api/equipe", token))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>();
        return equipe!.Single().Id;
    }

    private async Task<int> CriarClienteAsync(string token, string nome = "Maria")
    {
        var r = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome,
            telefone = $"(11) 9{Random.Shared.Next(1000, 9999)}-0000",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        return (await r.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;
    }

    private async Task<int> CriarServicoAsync(string token, string nome = "Troca de tela")
    {
        var r = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome,
            categoria = "Reparo",
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
        });
        return (await r.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<OrdemServicoResponse> CriarOsAsync(
        string token, int clienteId, int servicoId, Guid? tecnicoId = null)
    {
        var r = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
            descricaoProblema = "x",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = tecnicoId,
            observacoes = (string?)null,
        });
        return (await r.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task EntregarAsync(string token, Guid osId) =>
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/etapa", token,
            new { paraEtapa = "Entregue", motivo = (string?)null });

    private async Task<AcompanhamentoResponse> AcompanharAsync(string slug, string codigo) =>
        (await (await _cliente.GetAsync($"/api/publico/{slug}/acompanhar/{codigo}"))
            .Content.ReadFromJsonAsync<AcompanhamentoResponse>())!;

    private Task<HttpResponseMessage> AvaliarAsync(
        string slug, string codigo, int nota, int recomendacao, string? comentario = null) =>
        _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/acompanhar/{codigo}/avaliacao",
            new { nota, recomendacao, comentario });

    [Fact]
    public async Task SoAvaliaAposEntregaEUmaVezPorOs()
    {
        var (token, slug) = await RegistrarLojaAsync($"aval.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token);
        var os = await CriarOsAsync(token, cliente, servico);

        // Antes da entrega: não pode avaliar.
        var antes = await AcompanharAsync(slug, os.CodigoAcompanhamento);
        Assert.False(antes.PodeAvaliar);
        Assert.False(antes.JaAvaliada);
        var recusada = await AvaliarAsync(slug, os.CodigoAcompanhamento, 5, 10);
        Assert.Equal(HttpStatusCode.BadRequest, recusada.StatusCode);

        await EntregarAsync(token, os.Id);
        var depois = await AcompanharAsync(slug, os.CodigoAcompanhamento);
        Assert.True(depois.PodeAvaliar);

        var ok = await AvaliarAsync(slug, os.CodigoAcompanhamento, 5, 10, "Excelente");
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

        // Uma por OS: a segunda é recusada, e o acompanhamento marca avaliada.
        var segunda = await AvaliarAsync(slug, os.CodigoAcompanhamento, 4, 8);
        Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
        var final = await AcompanharAsync(slug, os.CodigoAcompanhamento);
        Assert.True(final.JaAvaliada);
        Assert.False(final.PodeAvaliar);
    }

    [Fact]
    public async Task EntregaDisparaPedidoDeAvaliacaoNaAuditoria()
    {
        var (token, _) = await RegistrarLojaAsync($"gatilho.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token);
        var os = await CriarOsAsync(token, cliente, servico);
        await EntregarAsync(token, os.Id);

        // O registro de comunicação do evento PedidoAvaliacao existe (auditoria por OS).
        var mensagens = await (await EnviarAsync(
                HttpMethod.Get, $"/api/ordens-servico/{os.Id}/mensagens", token))
            .Content.ReadAsStringAsync();
        Assert.Contains("PedidoAvaliacao", mensagens);
    }

    [Fact]
    public async Task ResumoCalculaMediaEstrelasNpsEPorTecnico()
    {
        var (token, slug) = await RegistrarLojaAsync($"resumo.{Guid.NewGuid():N}@exemplo.com");
        var tecnico = await GuidDoDonoAsync(token);
        var servico = await CriarServicoAsync(token);

        async Task AvaliarOsAsync(int nota, int recomendacao)
        {
            var cliente = await CriarClienteAsync(token);
            var os = await CriarOsAsync(token, cliente, servico, tecnico);
            await EntregarAsync(token, os.Id);
            var r = await AvaliarAsync(slug, os.CodigoAcompanhamento, nota, recomendacao);
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        // Notas 5,4,1 → média 3,3. Recomendações 10(prom),8(neutro),3(detrator)
        // → NPS = (1 promotor - 1 detrator)/3 = 0.
        await AvaliarOsAsync(5, 10);
        await AvaliarOsAsync(4, 8);
        await AvaliarOsAsync(1, 3);

        var resumo = await (await EnviarAsync(HttpMethod.Get, "/api/avaliacoes/resumo", token))
            .Content.ReadFromJsonAsync<ResumoAvaliacoesResponse>();
        Assert.Equal(3, resumo!.Total);
        Assert.Equal(3.3m, resumo.MediaEstrelas);
        Assert.Equal(1, resumo.Nps.Promotores);
        Assert.Equal(1, resumo.Nps.Detratores);
        Assert.Equal(0, resumo.Nps.Score);
        Assert.Equal(1, resumo.PendenciasLoop); // a nota 1 / recomendação 3
        var doTecnico = Assert.Single(resumo.PorTecnico);
        Assert.Equal(3, doTecnico.Total);
        Assert.Equal(3.3m, doTecnico.MediaEstrelas);
    }

    [Fact]
    public async Task AvaliacaoNegativaAbreLoopEPodeSerResolvida()
    {
        var (token, slug) = await RegistrarLojaAsync($"loop.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token);
        var os = await CriarOsAsync(token, cliente, servico);
        await EntregarAsync(token, os.Id);
        await AvaliarAsync(slug, os.CodigoAcompanhamento, 1, 2, "Demorou demais");

        // Aparece nas pendências.
        var pendentes = await (await EnviarAsync(
                HttpMethod.Get, "/api/avaliacoes?apenasPendentes=true", token))
            .Content.ReadFromJsonAsync<List<AvaliacaoResponse>>();
        var pendente = Assert.Single(pendentes!);
        Assert.True(pendente.Negativa);
        Assert.False(pendente.Resolvida);

        // Resolver exige nota de tratamento.
        var semNota = await EnviarAsync(
            HttpMethod.Post, $"/api/avaliacoes/{pendente.Id}/resolver", token, new { nota = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, semNota.StatusCode);

        var resolver = await EnviarAsync(
            HttpMethod.Post, $"/api/avaliacoes/{pendente.Id}/resolver", token,
            new { nota = "Liguei, reembolsei a taxa e o cliente voltou satisfeito." });
        Assert.Equal(HttpStatusCode.OK, resolver.StatusCode);
        var resolvida = (await resolver.Content.ReadFromJsonAsync<AvaliacaoResponse>())!;
        Assert.True(resolvida.Resolvida);
        Assert.NotNull(resolvida.ResolvidaEm);

        // Saiu das pendências e não resolve de novo.
        Assert.Empty(await (await EnviarAsync(
                HttpMethod.Get, "/api/avaliacoes?apenasPendentes=true", token))
            .Content.ReadFromJsonAsync<List<AvaliacaoResponse>>());
        var denovo = await EnviarAsync(
            HttpMethod.Post, $"/api/avaliacoes/{pendente.Id}/resolver", token,
            new { nota = "de novo" });
        Assert.Equal(HttpStatusCode.BadRequest, denovo.StatusCode);
    }

    [Fact]
    public async Task AvaliacaoPositivaNaoAbreLoop()
    {
        var (token, slug) = await RegistrarLojaAsync($"pos.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token);
        var os = await CriarOsAsync(token, cliente, servico);
        await EntregarAsync(token, os.Id);
        await AvaliarAsync(slug, os.CodigoAcompanhamento, 5, 10);

        var todas = await (await EnviarAsync(HttpMethod.Get, "/api/avaliacoes", token))
            .Content.ReadFromJsonAsync<List<AvaliacaoResponse>>();
        var a = Assert.Single(todas!);
        Assert.False(a.Negativa);
        // Não dá para "resolver" uma positiva.
        var r = await EnviarAsync(
            HttpMethod.Post, $"/api/avaliacoes/{a.Id}/resolver", token, new { nota = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task AvaliacoesNaoAtravessamEmpresas()
    {
        var (tokenA, slugA) = await RegistrarLojaAsync($"avalA.{Guid.NewGuid():N}@exemplo.com");
        var (tokenB, _) = await RegistrarLojaAsync($"avalB.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(tokenA);
        var servico = await CriarServicoAsync(tokenA);
        var os = await CriarOsAsync(tokenA, cliente, servico);
        await EntregarAsync(tokenA, os.Id);
        await AvaliarAsync(slugA, os.CodigoAcompanhamento, 1, 1);

        // B não vê nem a lista nem o resumo de A.
        Assert.Empty(await (await EnviarAsync(HttpMethod.Get, "/api/avaliacoes", tokenB))
            .Content.ReadFromJsonAsync<List<AvaliacaoResponse>>());
        var resumoB = await (await EnviarAsync(HttpMethod.Get, "/api/avaliacoes/resumo", tokenB))
            .Content.ReadFromJsonAsync<ResumoAvaliacoesResponse>();
        Assert.Equal(0, resumoB!.Total);
    }
}
