using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.OrdensServico;

public class OsFluxoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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
        HttpMethod metodo, string url, string token, object? corpo = null,
        string? chaveIdempotencia = null)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (chaveIdempotencia is not null)
        {
            requisicao.Headers.Add("Idempotency-Key", chaveIdempotencia);
        }

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return await _cliente.SendAsync(requisicao);
    }

    private async Task<int> CriarClienteAsync(string token, string nome = "Maria Souza")
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome,
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

    private async Task<int> CriarServicoAsync(string token)
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
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private static object CorpoOs(int clienteId, int servicoId) => new
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
    };

    private async Task<OrdemServicoResponse> CriarOsAsync(
        string token, int clienteId, int servicoId, string? chave = null)
    {
        var resposta = await EnviarAsync(
            HttpMethod.Post, "/api/ordens-servico", token, CorpoOs(clienteId, servicoId), chave);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    [Fact]
    public async Task CriacaoManualComNumeroSequencialEHistorico()
    {
        var token = await RegistrarEmpresaAsync("os.criacao@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servicoId = await CriarServicoAsync(token);

        var primeira = await CriarOsAsync(token, clienteId, servicoId);
        var segunda = await CriarOsAsync(token, clienteId, servicoId);

        Assert.Equal(1, primeira.Numero);
        Assert.Equal(2, segunda.Numero);
        Assert.Equal(EtapaOrdemServico.CheckInRealizado, primeira.Etapa);
        Assert.Equal(16, primeira.CodigoAcompanhamento.Length);
        Assert.Equal("Maria Souza", primeira.ClienteNome);

        var detalhe = await EnviarAsync(
            HttpMethod.Get, $"/api/ordens-servico/{primeira.Id}", token);
        Assert.Equal(HttpStatusCode.OK, detalhe.StatusCode);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        var entrada = Assert.Single(corpo!.Historico);
        Assert.Null(entrada.DeEtapa);
        Assert.Equal(EtapaOrdemServico.CheckInRealizado, entrada.ParaEtapa);
        Assert.Equal("Dono", entrada.UsuarioNome);
    }

    [Fact]
    public async Task IdempotencyKeyNaoDuplicaAOs()
    {
        var token = await RegistrarEmpresaAsync("os.idempotencia@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servicoId = await CriarServicoAsync(token);

        var primeira = await CriarOsAsync(token, clienteId, servicoId, chave: "retry-abc-123");
        var repetida = await CriarOsAsync(token, clienteId, servicoId, chave: "retry-abc-123");
        Assert.Equal(primeira.Id, repetida.Id);

        var lista = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", token);
        Assert.Single((await lista.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);
    }

    [Fact]
    public async Task MudancaDeEtapaGravaHistoricoECancelamentoExigeMotivo()
    {
        var token = await RegistrarEmpresaAsync("os.etapas@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        var mover = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "NaFila", motivo = (string?)null });
        Assert.Equal(HttpStatusCode.OK, mover.StatusCode);

        // Mesma etapa → 400.
        var repetido = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "NaFila", motivo = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);

        // Cancelar sem motivo → 400 de validação; com motivo → ok.
        var semMotivo = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "Cancelado", motivo = "" });
        Assert.Equal(HttpStatusCode.BadRequest, semMotivo.StatusCode);

        var cancelar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "Cancelado", motivo = "Cliente desistiu" });
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        var cancelada = await cancelar.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.Equal("Cliente desistiu", cancelada!.MotivoCancelamento);

        // Trilha completa: criação + NaFila + Cancelado.
        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(3, corpo!.Historico.Count);
        Assert.Equal(EtapaOrdemServico.NaFila, corpo.Historico[2].DeEtapa);

        // Finalizadas somem da listagem padrão e voltam com o filtro.
        var padrao = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", token);
        Assert.Empty((await padrao.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);
        var todas = await EnviarAsync(
            HttpMethod.Get, "/api/ordens-servico?incluirFinalizadas=true", token);
        Assert.Single((await todas.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);
    }

    [Fact]
    public async Task AtualizacaoDeCamposEValidacaoDeReferencias()
    {
        var token = await RegistrarEmpresaAsync("os.edicao@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        var equipe = await EnviarAsync(HttpMethod.Get, "/api/equipe", token);
        Assert.Equal(HttpStatusCode.OK, equipe.StatusCode);
        var membro = Assert.Single(
            (await equipe.Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!);

        var editar = await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}", token, new
        {
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "Galaxy A54",
            descricaoProblema = "Tela trincada e bateria",
            prioridade = "Alta",
            prazoEstimado = "2026-08-01",
            responsavelTecnicoId = membro.Id,
            statusPagamento = "Parcial",
            statusAprovacao = "Aprovado",
            observacoes = "Peça encomendada",
        });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        var editada = await editar.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        Assert.Equal(PrioridadeOrdemServico.Alta, editada!.Prioridade);
        Assert.Equal("Dono", editada.ResponsavelTecnicoNome);
        Assert.Equal(StatusPagamentoOrdemServico.Parcial, editada.StatusPagamento);
        Assert.Equal(StatusAprovacaoOrdemServico.Aprovado, editada.StatusAprovacao);

        // Responsável de outra empresa → 400 (anti-IDOR em tabela sem GQF).
        var outroToken = await RegistrarEmpresaAsync("os.edicao.outra@exemplo.com");
        var outraEquipe = await EnviarAsync(HttpMethod.Get, "/api/equipe", outroToken);
        var intruso = Assert.Single(
            (await outraEquipe.Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!);

        var invalido = await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}", token, new
        {
            aparelhoId = (int?)null,
            aparelhoMarca = (string?)null,
            aparelhoModelo = (string?)null,
            descricaoProblema = (string?)null,
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = intruso.Id,
            statusPagamento = "NaoPago",
            statusAprovacao = "Pendente",
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task CheckInDoAgendamentoCriaOsAutomaticamente()
    {
        var token = await RegistrarEmpresaAsync("os.conversao@exemplo.com");
        var servicoId = await CriarServicoAsync(token);

        var horarios = await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new
        {
            dias = Enumerable.Range(0, 7).Select(d => new
            {
                diaSemana = d,
                ativo = true,
                abertura = "09:00:00",
                fechamento = "18:00:00",
                intervaloInicio = (string?)null,
                intervaloFim = (string?)null,
            }).ToList(),
        });
        Assert.Equal(HttpStatusCode.OK, horarios.StatusCode);

        // Agendamento avulso (sem cliente do CRM): a conversão usa o vínculo
        // silencioso por telefone e cria o cliente.
        var data = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14);
        var agendar = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId,
            data = data.ToString("yyyy-MM-dd"),
            horaInicio = "10:00:00",
            nomeContato = "Walk-in Wanda",
            telefoneContato = "(31) 96666-5555",
            aparelhoMarca = "Xiaomi",
            aparelhoModelo = "Redmi Note 13",
            descricaoProblema = "Conector de carga",
        });
        Assert.Equal(HttpStatusCode.Created, agendar.StatusCode);
        var agendamento = await agendar.Content
            .ReadFromJsonAsync<TechPro.Api.Modules.Agendamentos.Dtos.AgendamentoResponse>();

        var checkin = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{agendamento!.Id}/checkin", token);
        Assert.Equal(HttpStatusCode.OK, checkin.StatusCode);

        var lista = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", token);
        var os = Assert.Single(
            (await lista.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);
        Assert.Equal(EtapaOrdemServico.CheckInRealizado, os.Etapa);
        Assert.Equal(agendamento.Id, os.AgendamentoId);
        Assert.Equal("Walk-in Wanda", os.ClienteNome);
        Assert.Equal("Xiaomi", os.AparelhoMarca);
        Assert.Equal("Conector de carga", os.DescricaoProblema);

        // O cliente nasceu no CRM pelo vínculo silencioso.
        var clientes = await EnviarAsync(HttpMethod.Get, "/api/clientes?busca=96666", token);
        var pagina = await clientes.Content
            .ReadFromJsonAsync<TechPro.Api.Shared.Api.PaginaResponse<ClienteResponse>>();
        Assert.Equal(1, pagina!.Total);
    }

    [Fact]
    public async Task SyncPorDeltaRetornaSoOQueMudou()
    {
        var token = await RegistrarEmpresaAsync("os.sync@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        var inicial = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico/sync", token);
        Assert.Equal(HttpStatusCode.OK, inicial.StatusCode);
        var delta = await inicial.Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        Assert.Single(delta!.Ordens);
        Assert.Single(delta.Historico);
        var marca = delta.Agora;

        // Nada mudou desde a marca → delta vazio.
        var vazio = await EnviarAsync(
            HttpMethod.Get, $"/api/ordens-servico/sync?since={Uri.EscapeDataString(marca.ToString("O"))}", token);
        var deltaVazio = await vazio.Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        Assert.Empty(deltaVazio!.Ordens);
        Assert.Empty(deltaVazio.Historico);

        // Mover etapa → a OS e a nova entrada de histórico entram no delta.
        var mover = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "EmDiagnostico", motivo = (string?)null });
        Assert.Equal(HttpStatusCode.OK, mover.StatusCode);

        var depois = await EnviarAsync(
            HttpMethod.Get, $"/api/ordens-servico/sync?since={Uri.EscapeDataString(marca.ToString("O"))}", token);
        var deltaDepois = await depois.Content.ReadFromJsonAsync<OrdensServicoSyncResponse>();
        Assert.Single(deltaDepois!.Ordens);
        Assert.Equal(EtapaOrdemServico.EmDiagnostico, deltaDepois.Ordens[0].Ordem.Etapa);
        Assert.Single(deltaDepois.Historico);
    }
}
