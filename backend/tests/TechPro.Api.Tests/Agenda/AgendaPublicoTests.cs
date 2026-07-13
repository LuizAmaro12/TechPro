using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Agenda;

/// <summary>
/// Rota pública de agendamento: resolução por slug, vínculo silencioso por
/// telefone e garantia de que nada de outro tenant (ou do CRM) vaza.
/// </summary>
public class AgendaPublicoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<int> CriarServicoAsync(string token, string nome, bool agendavelOnline)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome,
            categoria = "Reparo",
            precoBase = 200.00,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline,
            capacidadeSimultanea = 1,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var json = await resposta.Content.ReadFromJsonAsync<TechPro.Api.Modules.ServicosEPecas.Dtos.ServicoResponse>();
        return json!.Id;
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

    private static object CorpoPublico(int servicoId, string hora = "09:00:00",
        string nome = "Visitante Novo", string telefone = "(21) 97777-1234") => new
    {
        servicoId,
        data = DataFutura.ToString("yyyy-MM-dd"),
        horaInicio = hora,
        nomeContato = nome,
        telefoneContato = telefone,
        emailContato = (string?)null,
        descricaoProblema = "Não liga",
        aparelhoMarca = "Motorola",
        aparelhoModelo = "Moto G84",
    };

    [Fact]
    public async Task InfoPorSlugListaApenasServicosAgendaveisOnline()
    {
        var (token, slug) = await RegistrarLojaProntaAsync("publico.info@exemplo.com");
        await CriarServicoAsync(token, "Troca de tela", agendavelOnline: true);
        await CriarServicoAsync(token, "Reparo de placa", agendavelOnline: false);

        // Sem nenhum token: a rota é pública.
        var resposta = await _cliente.GetAsync($"/api/publico/{slug}/info");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var loja = await resposta.Content.ReadFromJsonAsync<LojaPublicaResponse>();
        Assert.Equal(slug, loja!.Slug);
        Assert.Single(loja.Servicos);
        Assert.Equal("Troca de tela", loja.Servicos[0].Nome);

        var inexistente = await _cliente.GetAsync("/api/publico/loja-que-nao-existe/info");
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact]
    public async Task AgendamentoPublicoCriaClienteNovoNoCrm()
    {
        var (token, slug) = await RegistrarLojaProntaAsync("publico.novo@exemplo.com");
        var servicoId = await CriarServicoAsync(token, "Troca de bateria", agendavelOnline: true);

        var resposta = await _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/agendamentos", CorpoPublico(servicoId));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var confirmacao = await resposta.Content.ReadFromJsonAsync<AgendamentoPublicoResponse>();
        Assert.Equal("Troca de bateria", confirmacao!.ServicoNome);

        // O agendamento nasceu vinculado a um cliente novo do CRM.
        var agendamentos = await EnviarAsync(HttpMethod.Get, "/api/agendamentos", token);
        var lista = await agendamentos.Content.ReadFromJsonAsync<List<AgendamentoResponse>>();
        var agendamento = Assert.Single(lista!);
        Assert.Equal(OrigemAgendamento.Portal, agendamento.Origem);
        Assert.NotNull(agendamento.ClienteId);

        var clientes = await EnviarAsync(HttpMethod.Get, "/api/clientes", token);
        var pagina = await clientes.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        var criado = Assert.Single(pagina!.Itens);
        Assert.Equal("Visitante Novo", criado.Nome);
    }

    [Fact]
    public async Task AgendamentoPublicoVinculaClienteExistentePorTelefone()
    {
        var (token, slug) = await RegistrarLojaProntaAsync("publico.vinculo@exemplo.com");
        var servicoId = await CriarServicoAsync(token, "Limpeza interna", agendavelOnline: true);

        var criarCliente = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria Souza",
            telefone = "(11) 99999-0000",
            email = "maria@exemplo.com",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = true,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        Assert.Equal(HttpStatusCode.Created, criarCliente.StatusCode);
        var existente = await criarCliente.Content.ReadFromJsonAsync<ClienteResponse>();

        // Mesmo telefone digitado sem máscara: vincula silenciosamente.
        var resposta = await _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/agendamentos",
            CorpoPublico(servicoId, nome: "Maria S.", telefone: "11999990000"));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        // Nada do cadastro vaza na resposta pública.
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Maria Souza", corpo);
        Assert.DoesNotContain("maria@exemplo.com", corpo);

        var agendamentos = await EnviarAsync(HttpMethod.Get, "/api/agendamentos", token);
        var lista = await agendamentos.Content.ReadFromJsonAsync<List<AgendamentoResponse>>();
        Assert.Equal(existente!.Id, Assert.Single(lista!).ClienteId);

        // Nenhum cliente novo foi criado.
        var clientes = await EnviarAsync(HttpMethod.Get, "/api/clientes", token);
        var pagina = await clientes.Content.ReadFromJsonAsync<PaginaResponse<ClienteResponse>>();
        Assert.Equal(1, pagina!.Total);
    }

    [Fact]
    public async Task ServicoNaoAgendavelOnlineNaoApareceNemAgenda()
    {
        var (token, slug) = await RegistrarLojaProntaAsync("publico.offline@exemplo.com");
        var servicoId = await CriarServicoAsync(token, "Reparo de placa", agendavelOnline: false);

        var disponibilidade = await _cliente.GetAsync(
            $"/api/publico/{slug}/disponibilidade?servicoId={servicoId}&data={DataFutura:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.BadRequest, disponibilidade.StatusCode);

        var agendar = await _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/agendamentos", CorpoPublico(servicoId));
        Assert.Equal(HttpStatusCode.BadRequest, agendar.StatusCode);
    }

    [Fact]
    public async Task DataPassadaEValidacaoDeCamposRejeitadas()
    {
        var (token, slug) = await RegistrarLojaProntaAsync("publico.validacao@exemplo.com");
        var servicoId = await CriarServicoAsync(token, "Troca de conector", agendavelOnline: true);

        var passado = await _cliente.PostAsJsonAsync($"/api/publico/{slug}/agendamentos", new
        {
            servicoId,
            data = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-10).ToString("yyyy-MM-dd"),
            horaInicio = "09:00:00",
            nomeContato = "Alguém",
            telefoneContato = "(11) 90000-0000",
            aparelhoMarca = "Apple",
            aparelhoModelo = "iPhone 13",
        });
        Assert.Equal(HttpStatusCode.BadRequest, passado.StatusCode);

        var semNome = await _cliente.PostAsJsonAsync($"/api/publico/{slug}/agendamentos", new
        {
            servicoId,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = "09:00:00",
            nomeContato = "",
            telefoneContato = "(11) 90000-0000",
            aparelhoMarca = "Apple",
            aparelhoModelo = "iPhone 13",
        });
        Assert.Equal(HttpStatusCode.BadRequest, semNome.StatusCode);
    }
}
