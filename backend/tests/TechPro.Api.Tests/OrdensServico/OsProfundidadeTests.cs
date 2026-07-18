using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.OrdensServico;

/// <summary>
/// Etapa "OS/Kanban em profundidade" (Fase 2): SLA visual por etapa,
/// comentários internos e reatribuição de técnico com motivo.
/// </summary>
public class OsProfundidadeTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<ServicoResponse> CriarServicoAsync(string token, int? slaHoras)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 300.00,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = true,
            capacidadeSimultanea = 1,
            slaHoras,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!;
    }

    private async Task<OrdemServicoResponse> CriarOsAsync(string token, int clienteId, int servicoId)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "Galaxy A54",
            descricaoProblema = "Tela trincada",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task<Guid> IdDoDonoAsync(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Get, "/api/equipe", token);
        var equipe = (await resposta.Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!;
        return equipe.Single().Id;
    }

    // --- SLA visual ---------------------------------------------------------------

    [Fact]
    public async Task SlaDoServicoAcompanhaAOsEORelogioZeraAoMudarDeEtapa()
    {
        var token = await RegistrarEmpresaAsync($"sla.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, slaHoras: 24);
        Assert.Equal(24, servico.SlaHoras);

        var os = await CriarOsAsync(token, clienteId, servico.Id);

        // OS recém-criada: SLA do serviço presente e relógio praticamente zerado.
        Assert.Equal(24, os.SlaHoras);
        Assert.True(os.HorasNaEtapa < 0.1m, $"HorasNaEtapa inesperado: {os.HorasNaEtapa}");

        // Mudar de etapa reinicia a contagem (o SLA é por etapa, não por OS).
        var movida = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa", token,
            new { paraEtapa = "EmReparo", motivo = (string?)null });
        var depois = (await movida.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        Assert.True(depois.HorasNaEtapa < 0.1m);
        Assert.Equal(24, depois.SlaHoras);
    }

    [Fact]
    public async Task EtapaFinalNaoTemSlaEServicoSemSlaDeixaOCardNeutro()
    {
        var token = await RegistrarEmpresaAsync($"slafinal.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);

        // Serviço sem SLA configurado: nada a cobrar.
        var semSla = await CriarServicoAsync(token, slaHoras: null);
        var osSemSla = await CriarOsAsync(token, clienteId, semSla.Id);
        Assert.Null(osSemSla.SlaHoras);

        // Serviço com SLA, mas OS entregue: parar ali é o fim esperado do fluxo.
        var comSla = await CriarServicoAsync(token, slaHoras: 8);
        var os = await CriarOsAsync(token, clienteId, comSla.Id);
        var entregue = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa", token,
            new { paraEtapa = "Entregue", motivo = (string?)null });
        var final = (await entregue.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        Assert.Null(final.SlaHoras);
    }

    [Fact]
    public async Task SlaForaDaFaixaEhRejeitado()
    {
        var token = await RegistrarEmpresaAsync($"slaruim.{Guid.NewGuid():N}@exemplo.com");
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Serviço eterno",
            categoria = (string?)null,
            precoBase = 10.0,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            slaHoras = 0,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    // --- Comentários internos ------------------------------------------------------

    [Fact]
    public async Task ComentarioInternoApareceNoDetalheESaiPorSoftDelete()
    {
        var token = await RegistrarEmpresaAsync($"coment.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, slaHoras: null);
        var os = await CriarOsAsync(token, clienteId, servico.Id);

        var criada = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/comentarios", token,
            new { texto = "  Cola da tela chegou torta, refiz.  " });
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var comentario = (await criada.Content.ReadFromJsonAsync<ComentarioResponse>())!;
        Assert.Equal("Cola da tela chegou torta, refiz.", comentario.Texto);
        Assert.Equal("Dono", comentario.AutorNome);

        var detalhe = await (await EnviarAsync(
                HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token))
            .Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Single(detalhe!.Comentarios);

        // Soft-delete: some do detalhe, mas continua no delta como lápide.
        var removida = await EnviarAsync(
            HttpMethod.Delete,
            $"/api/ordens-servico/{os.Id}/comentarios/{comentario.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, removida.StatusCode);

        var lista = await (await EnviarAsync(
                HttpMethod.Get, $"/api/ordens-servico/{os.Id}/comentarios", token))
            .Content.ReadFromJsonAsync<List<ComentarioResponse>>();
        Assert.Empty(lista!);

        var sync = await (await EnviarAsync(HttpMethod.Get, "/api/ordens-servico/sync", token))
            .Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        var noSync = Assert.Single(sync!.Comentarios);
        Assert.Equal(comentario.Id, noSync.Id);
        Assert.NotNull(noSync.DeletedAt);
    }

    [Fact]
    public async Task ComentarioVazioEhRejeitadoEOsInexistenteDa404()
    {
        var token = await RegistrarEmpresaAsync($"comentruim.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, slaHoras: null);
        var os = await CriarOsAsync(token, clienteId, servico.Id);

        var vazio = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/comentarios", token,
            new { texto = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, vazio.StatusCode);

        var inexistente = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{Guid.NewGuid()}/comentarios", token,
            new { texto = "oi" });
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    /// <summary>Comentário é interno: o portal público não pode vazá-lo.</summary>
    [Fact]
    public async Task ComentarioNaoVazaNoAcompanhamentoPublico()
    {
        var token = await RegistrarEmpresaAsync($"vaza.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, slaHoras: null);
        var os = await CriarOsAsync(token, clienteId, servico.Id);
        await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/comentarios", token,
            new { texto = "SEGREDO INTERNO DA LOJA" });

        var configuracoes = await (await EnviarAsync(HttpMethod.Get, "/api/configuracoes/loja", token))
            .Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        var slug = configuracoes!.RootElement.GetProperty("slug").GetString();

        var publico = await _cliente.GetAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}");
        Assert.Equal(HttpStatusCode.OK, publico.StatusCode);
        var corpo = await publico.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SEGREDO INTERNO", corpo, StringComparison.OrdinalIgnoreCase);
    }

    // --- Reatribuição de técnico ---------------------------------------------------

    [Fact]
    public async Task ReatribuicaoTrocaOResponsavelEGravaATrilhaComMotivo()
    {
        var token = await RegistrarEmpresaAsync($"reatrib.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, slaHoras: null);
        var os = await CriarOsAsync(token, clienteId, servico.Id);
        Assert.Null(os.ResponsavelTecnicoId);

        var dono = await IdDoDonoAsync(token);
        var resposta = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/responsavel", token,
            new { responsavelTecnicoId = dono, motivo = "Técnico original de férias" });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var detalhe = (await resposta.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>())!;
        Assert.Equal(dono, detalhe.Ordem.ResponsavelTecnicoId);
        var troca = Assert.Single(detalhe.Reatribuicoes);
        Assert.Null(troca.DeUsuarioId);
        Assert.Equal(dono, troca.ParaUsuarioId);
        Assert.Equal("Dono", troca.ParaNome);
        Assert.Equal("Dono", troca.PorNome);
        Assert.Equal("Técnico original de férias", troca.Motivo);

        // Trilha é append-only: devolver a OS para "sem responsável" soma, não sobrescreve.
        var devolvida = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/responsavel", token,
            new { responsavelTecnicoId = (Guid?)null, motivo = "Voltou para a fila" });
        var depois = (await devolvida.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>())!;
        Assert.Null(depois.Ordem.ResponsavelTecnicoId);
        Assert.Equal(2, depois.Reatribuicoes.Count);
        Assert.Equal(dono, depois.Reatribuicoes[1].DeUsuarioId);
    }

    [Fact]
    public async Task ReatribuicaoExigeMotivoERecusaResponsavelRepetido()
    {
        var token = await RegistrarEmpresaAsync($"reatribruim.{Guid.NewGuid():N}@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, slaHoras: null);
        var os = await CriarOsAsync(token, clienteId, servico.Id);
        var dono = await IdDoDonoAsync(token);

        var semMotivo = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/responsavel", token,
            new { responsavelTecnicoId = dono, motivo = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, semMotivo.StatusCode);

        // A OS já está sem responsável — repetir não é uma troca.
        var semTroca = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/responsavel", token,
            new { responsavelTecnicoId = (Guid?)null, motivo = "sem efeito" });
        Assert.Equal(HttpStatusCode.BadRequest, semTroca.StatusCode);
    }

    /// <summary>
    /// usuarios não tem Global Query Filter: sem a checagem manual de tenant, a
    /// OS de uma loja aceitaria o técnico de outra (IDOR).
    /// </summary>
    [Fact]
    public async Task ReatribuicaoRecusaTecnicoDeOutraEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync($"idorA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"idorB.{Guid.NewGuid():N}@exemplo.com");
        var donoDeB = await IdDoDonoAsync(tokenB);

        var clienteId = await CriarClienteAsync(tokenA);
        var servico = await CriarServicoAsync(tokenA, slaHoras: null);
        var os = await CriarOsAsync(tokenA, clienteId, servico.Id);

        var resposta = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/responsavel", tokenA,
            new { responsavelTecnicoId = donoDeB, motivo = "tentativa de IDOR" });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    /// <summary>Comentário de uma loja não existe para a outra (GQF + RLS).</summary>
    [Fact]
    public async Task ComentarioDeOutraEmpresaNaoEhAlcancavel()
    {
        var tokenA = await RegistrarEmpresaAsync($"isolA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"isolB.{Guid.NewGuid():N}@exemplo.com");

        var clienteId = await CriarClienteAsync(tokenA);
        var servico = await CriarServicoAsync(tokenA, slaHoras: null);
        var os = await CriarOsAsync(tokenA, clienteId, servico.Id);
        var comentario = (await (await EnviarAsync(
                HttpMethod.Post, $"/api/ordens-servico/{os.Id}/comentarios", tokenA,
                new { texto = "interno da A" }))
            .Content.ReadFromJsonAsync<ComentarioResponse>())!;

        var lendo = await EnviarAsync(
            HttpMethod.Get, $"/api/ordens-servico/{os.Id}/comentarios", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, lendo.StatusCode);

        var apagando = await EnviarAsync(
            HttpMethod.Delete,
            $"/api/ordens-servico/{os.Id}/comentarios/{comentario.Id}", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, apagando.StatusCode);

        var syncDeB = await (await EnviarAsync(HttpMethod.Get, "/api/ordens-servico/sync", tokenB))
            .Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        Assert.Empty(syncDeB!.Comentarios);
    }
}
