using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Agenda;

/// <summary>
/// Isolamento multi-tenant da agenda, inclusive no fluxo público: o slug de
/// uma loja nunca dá acesso a serviços/agendamentos de outra.
/// </summary>
public class AgendaIsolamentoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private static DateOnly DataFutura => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14);

    private async Task<(string Token, string Slug)> RegistrarLojaProntaAsync(string email)
    {
        var registro = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = $"Loja de {email}",
            nome = "Dono",
            email,
            senha = "senha123",
        });
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var auth = await registro.Content.ReadFromJsonAsync<AuthResponse>();
        var token = auth!.AccessToken;

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

    private async Task<int> CriarServicoAgendavelAsync(string token)
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
        var servico = await resposta.Content
            .ReadFromJsonAsync<TechPro.Api.Modules.ServicosEPecas.Dtos.ServicoResponse>();
        return servico!.Id;
    }

    [Fact]
    public async Task AgendamentosNaoVazamEntreEmpresas()
    {
        var (tokenA, _) = await RegistrarLojaProntaAsync("agenda.iso.a@exemplo.com");
        var (tokenB, _) = await RegistrarLojaProntaAsync("agenda.iso.b@exemplo.com");
        var servicoA = await CriarServicoAgendavelAsync(tokenA);

        var criar = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", tokenA, new
        {
            servicoId = servicoA,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = "09:00:00",
            nomeContato = "Cliente da A",
            telefoneContato = "(11) 90000-1111",
        });
        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
        var agendamento = await criar.Content.ReadFromJsonAsync<AgendamentoResponse>();

        // B não vê nem alcança o agendamento da A.
        var listaB = await EnviarAsync(HttpMethod.Get, "/api/agendamentos", tokenB);
        Assert.Empty((await listaB.Content.ReadFromJsonAsync<List<AgendamentoResponse>>())!);

        var checkinB = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{agendamento!.Id}/checkin", tokenB);
        Assert.Equal(HttpStatusCode.NotFound, checkinB.StatusCode);

        // B também não usa um serviço da A no agendamento manual (GQF → 400).
        var manualComServicoAlheio = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", tokenB, new
        {
            servicoId = servicoA,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = "10:00:00",
            nomeContato = "Intruso",
            telefoneContato = "(11) 92222-3333",
        });
        Assert.Equal(HttpStatusCode.BadRequest, manualComServicoAlheio.StatusCode);
    }

    [Fact]
    public async Task RotaPublicaNaoCruzaTenantsPeloSlug()
    {
        var (tokenA, _) = await RegistrarLojaProntaAsync("publico.iso.a@exemplo.com");
        var (_, slugB) = await RegistrarLojaProntaAsync("publico.iso.b@exemplo.com");
        var servicoA = await CriarServicoAgendavelAsync(tokenA);

        // Slug da loja B + serviço da loja A: para o tenant B esse serviço não existe.
        var info = await _cliente.GetAsync($"/api/publico/{slugB}/info");
        var loja = await info.Content.ReadFromJsonAsync<LojaPublicaResponse>();
        Assert.Empty(loja!.Servicos);

        var disponibilidade = await _cliente.GetAsync(
            $"/api/publico/{slugB}/disponibilidade?servicoId={servicoA}&data={DataFutura:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.BadRequest, disponibilidade.StatusCode);

        var agendar = await _cliente.PostAsJsonAsync($"/api/publico/{slugB}/agendamentos", new
        {
            servicoId = servicoA,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = "09:00:00",
            nomeContato = "Visitante",
            telefoneContato = "(11) 95555-4444",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });
        Assert.Equal(HttpStatusCode.BadRequest, agendar.StatusCode);

        // E a loja A segue sem nenhum agendamento.
        var listaA = await EnviarAsync(HttpMethod.Get, "/api/agendamentos", tokenA);
        Assert.Empty((await listaA.Content.ReadFromJsonAsync<List<AgendamentoResponse>>())!);
    }
}
