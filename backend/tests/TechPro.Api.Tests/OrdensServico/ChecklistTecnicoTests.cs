using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.OrdensServico;

/// <summary>
/// Etapa "portal do técnico" (Fase 2): checklist técnico por OS, materializado
/// do template do serviço na criação e marcável na bancada.
/// </summary>
public class ChecklistTecnicoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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
        var r = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria",
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

    private async Task<int> CriarServicoAsync(string token, params string[] checklist)
    {
        var r = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 300.00,
            duracaoEstimadaMinutos = 60,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            slaHoras = (int?)null,
            ativo = true,
            checklist,
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<OrdemServicoResponse> CriarOsAsync(string token, int clienteId, int servicoId)
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
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task<List<ItemChecklistResponse>> ChecklistAsync(string token, Guid osId) =>
        (await (await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{osId}/checklist", token))
            .Content.ReadFromJsonAsync<List<ItemChecklistResponse>>())!;

    [Fact]
    public async Task ChecklistEMaterializadoDoTemplateNaCriacaoNaOrdemCerta()
    {
        var token = await RegistrarEmpresaAsync($"chk.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, "Testar touch", "Conferir câmera", "Limpar conector");
        var os = await CriarOsAsync(token, cliente, servico);

        var checklist = await ChecklistAsync(token, os.Id);
        Assert.Equal(3, checklist.Count);
        Assert.Equal(new[] { "Testar touch", "Conferir câmera", "Limpar conector" },
            checklist.Select(i => i.Descricao));
        Assert.All(checklist, i => Assert.False(i.Concluido));
        // Também vem no detalhe da OS.
        var detalhe = await (await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token))
            .Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(3, detalhe!.Checklist.Count);
    }

    [Fact]
    public async Task ServicoSemChecklistGeraOsSemItens()
    {
        var token = await RegistrarEmpresaAsync($"chkvazio.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token); // sem itens
        var os = await CriarOsAsync(token, cliente, servico);
        Assert.Empty(await ChecklistAsync(token, os.Id));
    }

    [Fact]
    public async Task MarcarEDesmarcarGravaAutoriaEEntraNoSync()
    {
        var token = await RegistrarEmpresaAsync($"chkmarca.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        var servico = await CriarServicoAsync(token, "Testar touch");
        var os = await CriarOsAsync(token, cliente, servico);
        var item = (await ChecklistAsync(token, os.Id)).Single();

        var marcado = await EnviarAsync(
            HttpMethod.Put, $"/api/ordens-servico/{os.Id}/checklist/{item.Id}", token,
            new { concluido = true });
        Assert.Equal(HttpStatusCode.OK, marcado.StatusCode);
        var apos = (await marcado.Content.ReadFromJsonAsync<ItemChecklistResponse>())!;
        Assert.True(apos.Concluido);
        Assert.Equal("Dono", apos.ConcluidoPorNome);
        Assert.NotNull(apos.ConcluidoEm);

        // Entra no delta (contrato offline do app do técnico).
        var sync = await (await EnviarAsync(HttpMethod.Get, "/api/ordens-servico/sync", token))
            .Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        var noSync = Assert.Single(sync!.Checklist);
        Assert.True(noSync.Concluido);
        Assert.Equal(item.Id, noSync.Id);

        // Desmarcar limpa a autoria.
        var desmarcado = (await (await EnviarAsync(
                HttpMethod.Put, $"/api/ordens-servico/{os.Id}/checklist/{item.Id}", token,
                new { concluido = false }))
            .Content.ReadFromJsonAsync<ItemChecklistResponse>())!;
        Assert.False(desmarcado.Concluido);
        Assert.Null(desmarcado.ConcluidoPorNome);
        Assert.Null(desmarcado.ConcluidoEm);
    }

    /// <summary>
    /// Atendente não trabalha na bancada — marcar o checklist é da política
    /// Bancada (gestor ou técnico).
    /// </summary>
    [Fact]
    public async Task AtendenteNaoMarcaChecklist()
    {
        var gestor = await RegistrarEmpresaAsync($"chkaten.{Guid.NewGuid():N}@exemplo.com");
        var emailAtendente = $"atd.{Guid.NewGuid():N}@exemplo.com";
        await EnviarAsync(HttpMethod.Post, "/api/equipe", gestor, new
        {
            nome = "Atendente",
            email = emailAtendente,
            senha = "senha123",
            papel = Papeis.Atendente,
        });
        var login = await _cliente.PostAsJsonAsync(
            "/api/auth/login", new { email = emailAtendente, senha = "senha123" });
        var tokenAtendente = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;

        var cliente = await CriarClienteAsync(gestor);
        var servico = await CriarServicoAsync(gestor, "Testar touch");
        var os = await CriarOsAsync(gestor, cliente, servico);
        var item = (await ChecklistAsync(gestor, os.Id)).Single();

        var tentativa = await EnviarAsync(
            HttpMethod.Put, $"/api/ordens-servico/{os.Id}/checklist/{item.Id}", tokenAtendente,
            new { concluido = true });
        Assert.Equal(HttpStatusCode.Forbidden, tentativa.StatusCode);
    }

    [Fact]
    public async Task ChecklistNaoAtravessaEmpresas()
    {
        var tokenA = await RegistrarEmpresaAsync($"chkA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"chkB.{Guid.NewGuid():N}@exemplo.com");
        var cliente = await CriarClienteAsync(tokenA);
        var servico = await CriarServicoAsync(tokenA, "Testar touch");
        var os = await CriarOsAsync(tokenA, cliente, servico);
        var item = (await ChecklistAsync(tokenA, os.Id)).Single();

        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}/checklist", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}/checklist/{item.Id}", tokenB,
                new { concluido = true })).StatusCode);
        var syncB = await (await EnviarAsync(HttpMethod.Get, "/api/ordens-servico/sync", tokenB))
            .Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        Assert.Empty(syncB!.Checklist);
    }

    /// <summary>A bancada filtra por responsável — a API já aceita o filtro.</summary>
    [Fact]
    public async Task ListagemPorResponsavelTrazSoAsOsDoTecnico()
    {
        var gestor = await RegistrarEmpresaAsync($"banca.{Guid.NewGuid():N}@exemplo.com");
        var equipe = await (await EnviarAsync(HttpMethod.Get, "/api/equipe", gestor))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>();
        var dono = equipe!.Single().Id;

        var cliente = await CriarClienteAsync(gestor);
        var servico = await CriarServicoAsync(gestor, "Testar touch");

        // Uma OS do dono, uma sem responsável.
        var doDono = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", gestor, new
        {
            clienteId = cliente,
            servicoId = servico,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
            descricaoProblema = "x",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = dono,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, doDono.StatusCode);
        var osDoDono = (await doDono.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
        await CriarOsAsync(gestor, cliente, servico); // sem responsável

        var minhas = (await (await EnviarAsync(
                HttpMethod.Get, $"/api/ordens-servico?responsavelId={dono}", gestor))
            .Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!;
        Assert.Single(minhas);
        Assert.Equal(osDoDono.Id, minhas[0].Id);
    }
}
