using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.OrdensServico;

/// <summary>
/// Isolamento multi-tenant da OS e acompanhamento público por código opaco.
/// </summary>
public class OsIsolamentoEPublicoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private async Task<(string Token, string Slug)> RegistrarEmpresaAsync(string email)
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

        var configuracao = await EnviarAsync(HttpMethod.Get, "/api/agenda/configuracoes", token);
        var atual = await configuracao.Content.ReadFromJsonAsync<ConfiguracaoAgendaResponse>();
        return (token, atual!.Slug);
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

    private async Task<OrdemServicoResponse> CriarOsCompletaAsync(string token)
    {
        var cliente = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Cliente da OS",
            telefone = "(11) 95555-0000",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        Assert.Equal(HttpStatusCode.Created, cliente.StatusCode);
        var clienteId = (await cliente.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;

        var servico = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de bateria",
            categoria = "Reparo",
            precoBase = 180.00,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, servico.StatusCode);
        var servicoId = (await servico.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;

        var os = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = "Apple",
            aparelhoModelo = "iPhone 13",
            descricaoProblema = "Bateria viciada",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, os.StatusCode);
        return (await os.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    [Fact]
    public async Task OsNaoVazamEntreEmpresas()
    {
        var (tokenA, _) = await RegistrarEmpresaAsync("os.iso.a@exemplo.com");
        var (tokenB, _) = await RegistrarEmpresaAsync("os.iso.b@exemplo.com");
        var osA = await CriarOsCompletaAsync(tokenA);

        var listaB = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", tokenB);
        Assert.Empty((await listaB.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);

        var detalheB = await EnviarAsync(
            HttpMethod.Get, $"/api/ordens-servico/{osA.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, detalheB.StatusCode);

        var moverB = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osA.Id}/etapa",
            tokenB, new { paraEtapa = "NaFila", motivo = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, moverB.StatusCode);

        // B não cria OS apontando para cliente da A (GQF → 400).
        var criarB = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", tokenB, new
        {
            clienteId = osA.ClienteId,
            servicoId = osA.ServicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = (string?)null,
            aparelhoModelo = (string?)null,
            descricaoProblema = (string?)null,
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, criarB.StatusCode);

        // O número sequencial é por empresa: a primeira OS da B nasce como nº 1.
        var osB = await CriarOsCompletaAsync(tokenB);
        Assert.Equal(1, osB.Numero);
    }

    [Fact]
    public async Task AcompanhamentoPublicoPorCodigoOpaco()
    {
        var (tokenA, slugA) = await RegistrarEmpresaAsync("os.publico.a@exemplo.com");
        var (_, slugB) = await RegistrarEmpresaAsync("os.publico.b@exemplo.com");
        var os = await CriarOsCompletaAsync(tokenA);

        // Sem token nenhum: página pública de status.
        var acompanhar = await _cliente.GetAsync(
            $"/api/publico/{slugA}/acompanhar/{os.CodigoAcompanhamento}");
        Assert.Equal(HttpStatusCode.OK, acompanhar.StatusCode);
        var status = await acompanhar.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.Equal(os.Numero, status!.Numero);
        Assert.Equal(EtapaOrdemServico.CheckInRealizado, status.Etapa);
        Assert.Equal("Troca de bateria", status.ServicoNome);

        // Não vaza dado pessoal.
        var corpo = await acompanhar.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Cliente da OS", corpo);
        Assert.DoesNotContain("95555", corpo);

        // Código certo no slug errado → 404 (tenant do slug não tem essa OS).
        var slugErrado = await _cliente.GetAsync(
            $"/api/publico/{slugB}/acompanhar/{os.CodigoAcompanhamento}");
        Assert.Equal(HttpStatusCode.NotFound, slugErrado.StatusCode);

        var codigoErrado = await _cliente.GetAsync(
            $"/api/publico/{slugA}/acompanhar/0000000000000000");
        Assert.Equal(HttpStatusCode.NotFound, codigoErrado.StatusCode);

        // A etapa muda → o público reflete.
        var mover = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            tokenA, new { paraEtapa = "EmReparo", motivo = (string?)null });
        Assert.Equal(HttpStatusCode.OK, mover.StatusCode);

        acompanhar = await _cliente.GetAsync(
            $"/api/publico/{slugA}/acompanhar/{os.CodigoAcompanhamento}");
        status = await acompanhar.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.Equal(EtapaOrdemServico.EmReparo, status!.Etapa);

        // Linha do tempo (Fase 2): as etapas percorridas, em ordem, cada uma
        // com o instante em que foi alcançada. A OS nasceu em CheckInRealizado
        // e foi movida para EmReparo.
        Assert.Equal(
            new[] { EtapaOrdemServico.CheckInRealizado, EtapaOrdemServico.EmReparo },
            status.LinhaDoTempo.Select(e => e.Etapa).ToArray());
        Assert.True(status.LinhaDoTempo[0].AlcancadaEm <= status.LinhaDoTempo[1].AlcancadaEm);
    }

    [Fact]
    public async Task LinhaDoTempoRegistraPrimeiraVezDeCadaEtapa()
    {
        var (token, slug) = await RegistrarEmpresaAsync("os.timeline@exemplo.com");
        var os = await CriarOsCompletaAsync(token);

        // Vai e volta: NaFila → EmReparo → NaFila (correção). A linha do tempo
        // guarda a 1ª vez que NaFila foi alcançada (não a revisita).
        foreach (var etapa in new[] { "NaFila", "EmReparo", "NaFila" })
        {
            Assert.Equal(HttpStatusCode.OK,
                (await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
                    token, new { paraEtapa = etapa, motivo = (string?)null })).StatusCode);
        }

        var resposta = await _cliente.GetAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}");
        var status = await resposta.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        // Etapas distintas (sem duplicar NaFila), na ordem da 1ª ocorrência.
        Assert.Equal(
            new[]
            {
                EtapaOrdemServico.CheckInRealizado,
                EtapaOrdemServico.NaFila,
                EtapaOrdemServico.EmReparo,
            },
            status!.LinhaDoTempo.Select(e => e.Etapa).ToArray());
    }
}
