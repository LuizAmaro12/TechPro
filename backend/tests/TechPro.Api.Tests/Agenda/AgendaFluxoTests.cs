using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Agenda;

public class AgendaFluxoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    // Data fixa no futuro: os testes não dependem do relógio para achar vaga.
    private static DateOnly DataFutura => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14);

    private async Task<string> RegistrarEmpresaAsync(string email, string? nomeEmpresa = null)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = nomeEmpresa ?? $"Loja de {email}",
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

    private static object DiaAtivo(int diaSemana, bool comIntervalo = true) => new
    {
        diaSemana,
        ativo = true,
        abertura = "09:00:00",
        fechamento = "18:00:00",
        intervaloInicio = comIntervalo ? "12:00:00" : null,
        intervaloFim = comIntervalo ? "13:00:00" : null,
    };

    /// <summary>Semana inteira aberta 09–18 com almoço 12–13.</summary>
    private async Task ConfigurarSemanaAsync(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new
        {
            dias = Enumerable.Range(0, 7).Select(d => DiaAtivo(d)).ToList(),
        });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private async Task<ServicoResponse> CriarServicoAsync(
        string token, string nome = "Troca de tela", int duracao = 60,
        int capacidade = 1, bool agendavelOnline = true)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome,
            categoria = "Reparo",
            precoBase = 350.00,
            duracaoEstimadaMinutos = duracao,
            prazoMedioDias = 2,
            exigeDiagnostico = false,
            agendavelOnline,
            capacidadeSimultanea = capacidade,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!;
    }

    private async Task<AgendamentoResponse> CriarAgendamentoAsync(
        string token, int servicoId, DateOnly data, string horaInicio,
        int? clienteId = null, string? nomeContato = "Carlos Cliente",
        string? telefoneContato = "(11) 98888-7777")
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId,
            data = data.ToString("yyyy-MM-dd"),
            horaInicio,
            clienteId,
            nomeContato,
            telefoneContato,
            emailContato = (string?)null,
            descricaoProblema = "Tela trincada",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "Galaxy A54",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<AgendamentoResponse>())!;
    }

    [Fact]
    public async Task HorariosComecamFechadosEPutConfiguraSemana()
    {
        var token = await RegistrarEmpresaAsync("agenda.horarios@exemplo.com");

        var inicial = await EnviarAsync(HttpMethod.Get, "/api/agenda/horarios", token);
        Assert.Equal(HttpStatusCode.OK, inicial.StatusCode);
        var dias = await inicial.Content.ReadFromJsonAsync<List<HorarioFuncionamentoDia>>();
        Assert.Equal(7, dias!.Count);
        Assert.All(dias, d => Assert.False(d.Ativo));

        await ConfigurarSemanaAsync(token);

        var depois = await EnviarAsync(HttpMethod.Get, "/api/agenda/horarios", token);
        dias = await depois.Content.ReadFromJsonAsync<List<HorarioFuncionamentoDia>>();
        Assert.All(dias!, d =>
        {
            Assert.True(d.Ativo);
            Assert.Equal(new TimeOnly(9, 0), d.Abertura);
            Assert.Equal(new TimeOnly(18, 0), d.Fechamento);
        });
    }

    [Fact]
    public async Task PutHorariosIncompletoRetorna400()
    {
        var token = await RegistrarEmpresaAsync("agenda.horarios.invalido@exemplo.com");
        var resposta = await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new
        {
            dias = Enumerable.Range(0, 6).Select(d => DiaAtivo(d)).ToList(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task DisponibilidadeRespeitaHorarioIntervaloEBloqueio()
    {
        var token = await RegistrarEmpresaAsync("agenda.disponibilidade@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token, duracao: 60);
        var data = DataFutura;

        var resposta = await EnviarAsync(
            HttpMethod.Get, $"/api/agenda/disponibilidade?servicoId={servico.Id}&data={data:yyyy-MM-dd}", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var disponibilidade = await resposta.Content.ReadFromJsonAsync<DisponibilidadeResponse>();

        // 09–18 com almoço 12–13 e 60 min: manhã 09:00–11:00, tarde 13:00–17:00.
        Assert.Equal(60, disponibilidade!.DuracaoMinutos);
        Assert.Equal(14, disponibilidade.HorariosLivres.Count);
        Assert.Contains(new TimeOnly(9, 0), disponibilidade.HorariosLivres);
        Assert.Contains(new TimeOnly(17, 0), disponibilidade.HorariosLivres);
        Assert.DoesNotContain(new TimeOnly(11, 30), disponibilidade.HorariosLivres);
        Assert.DoesNotContain(new TimeOnly(12, 0), disponibilidade.HorariosLivres);

        // Bloqueio 15:00–16:00 derruba as janelas que o tocam (14:30, 15:00, 15:30).
        var bloqueio = await EnviarAsync(HttpMethod.Post, "/api/agenda/bloqueios", token, new
        {
            data = data.ToString("yyyy-MM-dd"),
            horaInicio = "15:00:00",
            horaFim = "16:00:00",
            motivo = "Compromisso externo",
        });
        Assert.Equal(HttpStatusCode.Created, bloqueio.StatusCode);

        resposta = await EnviarAsync(
            HttpMethod.Get, $"/api/agenda/disponibilidade?servicoId={servico.Id}&data={data:yyyy-MM-dd}", token);
        disponibilidade = await resposta.Content.ReadFromJsonAsync<DisponibilidadeResponse>();
        Assert.Equal(11, disponibilidade!.HorariosLivres.Count);
        Assert.DoesNotContain(new TimeOnly(14, 30), disponibilidade.HorariosLivres);
        Assert.DoesNotContain(new TimeOnly(15, 0), disponibilidade.HorariosLivres);
        Assert.DoesNotContain(new TimeOnly(15, 30), disponibilidade.HorariosLivres);
        Assert.Contains(new TimeOnly(16, 0), disponibilidade.HorariosLivres);
    }

    [Fact]
    public async Task AgendamentoManualComCapacidadeAcoesEReagendamento()
    {
        var token = await RegistrarEmpresaAsync("agenda.fluxo@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token, capacidade: 1);
        var data = DataFutura;

        var agendamento = await CriarAgendamentoAsync(token, servico.Id, data, "10:00:00");
        Assert.Equal(StatusAgendamento.Agendado, agendamento.Status);
        Assert.Equal(OrigemAgendamento.Manual, agendamento.Origem);
        Assert.Equal(new TimeOnly(11, 0), agendamento.HoraFim);

        // Capacidade 1: mesmo horário de novo → 400.
        var conflito = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId = servico.Id,
            data = data.ToString("yyyy-MM-dd"),
            horaInicio = "10:00:00",
            nomeContato = "Outra Pessoa",
            telefoneContato = "(11) 91111-2222",
        });
        Assert.Equal(HttpStatusCode.BadRequest, conflito.StatusCode);

        // Reagendar para a tarde marca ReagendadoEm.
        var reagendar = await EnviarAsync(HttpMethod.Put, $"/api/agendamentos/{agendamento.Id}", token, new
        {
            servicoId = servico.Id,
            data = data.ToString("yyyy-MM-dd"),
            horaInicio = "14:00:00",
            nomeContato = "Carlos Cliente",
            telefoneContato = "(11) 98888-7777",
        });
        Assert.Equal(HttpStatusCode.OK, reagendar.StatusCode);
        var reagendado = await reagendar.Content.ReadFromJsonAsync<AgendamentoResponse>();
        Assert.NotNull(reagendado!.ReagendadoEm);
        Assert.Equal(new TimeOnly(14, 0), reagendado.HoraInicio);

        // Check-in e, depois dele, edição bloqueada.
        var checkin = await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{agendamento.Id}/checkin", token);
        Assert.Equal(HttpStatusCode.OK, checkin.StatusCode);
        var comCheckin = await checkin.Content.ReadFromJsonAsync<AgendamentoResponse>();
        Assert.Equal(StatusAgendamento.CheckInRealizado, comCheckin!.Status);

        var editarDepois = await EnviarAsync(HttpMethod.Put, $"/api/agendamentos/{agendamento.Id}", token, new
        {
            servicoId = servico.Id,
            data = data.ToString("yyyy-MM-dd"),
            horaInicio = "16:00:00",
            nomeContato = "Carlos Cliente",
            telefoneContato = "(11) 98888-7777",
        });
        Assert.Equal(HttpStatusCode.BadRequest, editarDepois.StatusCode);

        // Cancelar com motivo; cancelar de novo → 400.
        var cancelar = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{agendamento.Id}/cancelar", token,
            new { motivo = "Cliente desistiu" });
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        var cancelado = await cancelar.Content.ReadFromJsonAsync<AgendamentoResponse>();
        Assert.Equal(StatusAgendamento.Cancelado, cancelado!.Status);
        Assert.Equal("Cliente desistiu", cancelado.MotivoCancelamento);

        var cancelarDeNovo = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{agendamento.Id}/cancelar", token, new { motivo = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, cancelarDeNovo.StatusCode);

        // Horário cancelado volta a ficar livre.
        var disponibilidade = await EnviarAsync(
            HttpMethod.Get, $"/api/agenda/disponibilidade?servicoId={servico.Id}&data={data:yyyy-MM-dd}", token);
        var slots = await disponibilidade.Content.ReadFromJsonAsync<DisponibilidadeResponse>();
        Assert.Contains(new TimeOnly(14, 0), slots!.HorariosLivres);
    }

    [Fact]
    public async Task AgendamentoManualSemContatoNemClienteRetorna400()
    {
        var token = await RegistrarEmpresaAsync("agenda.validacao@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);

        var resposta = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId = servico.Id,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = "09:00:00",
        });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task AgendamentoComClienteVinculadoUsaContatoDoCadastro()
    {
        var token = await RegistrarEmpresaAsync("agenda.cliente@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);

        var criarCliente = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria Souza",
            telefone = "(11) 99999-0000",
            email = "maria@exemplo.com",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        Assert.Equal(HttpStatusCode.Created, criarCliente.StatusCode);
        var cliente = await criarCliente.Content.ReadFromJsonAsync<ClienteResponse>();

        var agendamento = await CriarAgendamentoAsync(
            token, servico.Id, DataFutura, "09:00:00",
            clienteId: cliente!.Id, nomeContato: null, telefoneContato: null);
        Assert.Equal(cliente.Id, agendamento.ClienteId);
        Assert.Equal("Maria Souza", agendamento.NomeContato);
        Assert.Equal("(11) 99999-0000", agendamento.TelefoneContato);
    }

    [Fact]
    public async Task SlugGeradoDoNomeEEditavelComConflito()
    {
        var token = await RegistrarEmpresaAsync(
            "agenda.slug@exemplo.com", nomeEmpresa: "Oficina do João & Cia");

        var configuracao = await EnviarAsync(HttpMethod.Get, "/api/agenda/configuracoes", token);
        Assert.Equal(HttpStatusCode.OK, configuracao.StatusCode);
        var atual = await configuracao.Content.ReadFromJsonAsync<ConfiguracaoAgendaResponse>();
        Assert.Equal("oficina-do-joao-cia", atual!.Slug);

        var editar = await EnviarAsync(HttpMethod.Put, "/api/agenda/configuracoes", token,
            new { slug = "oficina-do-joao-sp" });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        var outroToken = await RegistrarEmpresaAsync("agenda.slug2@exemplo.com");
        var conflito = await EnviarAsync(HttpMethod.Put, "/api/agenda/configuracoes", outroToken,
            new { slug = "oficina-do-joao-sp" });
        Assert.Equal(HttpStatusCode.Conflict, conflito.StatusCode);

        var invalido = await EnviarAsync(HttpMethod.Put, "/api/agenda/configuracoes", token,
            new { slug = "Slug Inválido!" });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task AgendaExigeAutenticacao()
    {
        var resposta = await _cliente.GetAsync("/api/agendamentos");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
