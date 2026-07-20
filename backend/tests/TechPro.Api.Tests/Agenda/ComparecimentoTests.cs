using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Agenda;

/// <summary>
/// Etapa "não comparecimento e histórico de comparecimento" (Fase 2).
/// </summary>
public class ComparecimentoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private static DateOnly DataFutura => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14);

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

    private async Task ConfigurarSemanaAsync(string token)
    {
        var dias = Enumerable.Range(0, 7).Select(d => new
        {
            diaSemana = d,
            ativo = true,
            abertura = "09:00:00",
            fechamento = "18:00:00",
            intervaloInicio = (string?)null,
            intervaloFim = (string?)null,
        }).ToList();
        var resposta = await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new { dias });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private async Task<int> CriarServicoAsync(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 350.00,
            duracaoEstimadaMinutos = 60,
            prazoMedioDias = 2,
            exigeDiagnostico = false,
            agendavelOnline = true,
            capacidadeSimultanea = 3,
            slaHoras = (int?)null,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<int> CriarClienteAsync(string token, string nome = "Maria Faltante")
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome,
            telefone = "(11) 90000-0000",
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

    private async Task<AgendamentoResponse> CriarAgendamentoAsync(
        string token, int servicoId, int? clienteId, string hora)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = hora,
            clienteId,
            nomeContato = "Maria Faltante",
            telefoneContato = "(11) 90000-0000",
            emailContato = (string?)null,
            descricaoProblema = "x",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<AgendamentoResponse>())!;
    }

    private async Task<ComparecimentoResponse> ComparecimentoAsync(string token, int clienteId) =>
        (await (await EnviarAsync(HttpMethod.Get, $"/api/clientes/{clienteId}/comparecimento", token))
            .Content.ReadFromJsonAsync<ComparecimentoResponse>())!;

    [Fact]
    public async Task MarcarFaltaSoValeDeAgendadoEProduzEstadoTerminalProprio()
    {
        var token = await RegistrarEmpresaAsync($"falta.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);
        var clienteId = await CriarClienteAsync(token);
        var ag = await CriarAgendamentoAsync(token, servico, clienteId, "10:00:00");

        var faltou = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{ag.Id}/nao-compareceu", token);
        Assert.Equal(HttpStatusCode.OK, faltou.StatusCode);
        var atualizado = (await faltou.Content.ReadFromJsonAsync<AgendamentoResponse>())!;
        Assert.Equal(StatusAgendamento.NaoCompareceu, atualizado.Status);
        // Falta não é cancelamento: o timestamp/motivo de cancelamento continua vazio.
        Assert.Null(atualizado.CanceladoEm);
        Assert.Null(atualizado.MotivoCancelamento);

        // Estado terminal: não dá para marcar falta de novo.
        var denovo = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{ag.Id}/nao-compareceu", token);
        Assert.Equal(HttpStatusCode.BadRequest, denovo.StatusCode);
    }

    [Fact]
    public async Task NaoDaParaMarcarFaltaDeAgendamentoJaCancelado()
    {
        var token = await RegistrarEmpresaAsync($"faltacancel.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);
        var ag = await CriarAgendamentoAsync(token, servico, null, "10:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{ag.Id}/cancelar", token,
            new { motivo = "cliente avisou" });

        var faltou = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{ag.Id}/nao-compareceu", token);
        Assert.Equal(HttpStatusCode.BadRequest, faltou.StatusCode);
    }

    [Fact]
    public async Task HistoricoDeComparecimentoAgregaCadaEstado()
    {
        var token = await RegistrarEmpresaAsync($"hist.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);
        var clienteId = await CriarClienteAsync(token);

        // Um compareceu (check-in), duas faltas, um cancelamento.
        var compareceu = await CriarAgendamentoAsync(token, servico, clienteId, "09:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{compareceu.Id}/checkin", token);

        var falta1 = await CriarAgendamentoAsync(token, servico, clienteId, "10:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{falta1.Id}/nao-compareceu", token);
        var falta2 = await CriarAgendamentoAsync(token, servico, clienteId, "11:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{falta2.Id}/nao-compareceu", token);

        var cancelou = await CriarAgendamentoAsync(token, servico, clienteId, "14:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{cancelou.Id}/cancelar", token,
            new { motivo = "remarcou" });

        var comp = await ComparecimentoAsync(token, clienteId);
        Assert.Equal(1, comp.Compareceu);
        Assert.Equal(2, comp.Faltou);
        Assert.Equal(1, comp.Cancelou);
        Assert.Equal(4, comp.Recentes.Count);
    }

    [Fact]
    public async Task AgendaSinalizaFaltasAnterioresDoClienteVinculado()
    {
        var token = await RegistrarEmpresaAsync($"sinal.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);
        var clienteId = await CriarClienteAsync(token);

        var falta = await CriarAgendamentoAsync(token, servico, clienteId, "09:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{falta.Id}/nao-compareceu", token);

        // Novo agendamento do mesmo cliente já nasce com o histórico de faltas.
        var proximo = await CriarAgendamentoAsync(token, servico, clienteId, "10:00:00");
        Assert.Equal(1, proximo.clienteFaltas);

        // Agendamento sem cliente vinculado não tem histórico.
        var avulso = await CriarAgendamentoAsync(token, servico, null, "11:00:00");
        Assert.Equal(0, avulso.clienteFaltas);

        // A listagem também traz o número por item.
        var lista = (await (await EnviarAsync(HttpMethod.Get, "/api/agendamentos", token))
            .Content.ReadFromJsonAsync<List<AgendamentoResponse>>())!;
        Assert.Equal(1, lista.Single(a => a.Id == proximo.Id).clienteFaltas);
        Assert.Equal(0, lista.Single(a => a.Id == avulso.Id).clienteFaltas);
    }

    [Fact]
    public async Task ComparecimentoNaoAtravessaEmpresas()
    {
        var tokenA = await RegistrarEmpresaAsync($"compA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"compB.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(tokenA);
        var servico = await CriarServicoAsync(tokenA);
        var clienteId = await CriarClienteAsync(tokenA);
        var falta = await CriarAgendamentoAsync(tokenA, servico, clienteId, "09:00:00");
        await EnviarAsync(HttpMethod.Post, $"/api/agendamentos/{falta.Id}/nao-compareceu", tokenA);

        // O cliente de A "não existe" para B (GQF/RLS) — resumo vem zerado.
        var compDeB = await ComparecimentoAsync(tokenB, clienteId);
        Assert.Equal(0, compDeB.Compareceu);
        Assert.Equal(0, compDeB.Faltou);
        Assert.Empty(compDeB.Recentes);

        // E B não consegue marcar falta no agendamento de A.
        var marcando = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{falta.Id}/nao-compareceu", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, marcando.StatusCode);
    }
}
