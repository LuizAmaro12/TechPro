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
/// Etapa "fila de espera" (Fase 2): captar a demanda que se perdia quando não
/// há vaga, e convertê-la em agendamento quando abre horário.
/// </summary>
public class FilaEsperaTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private static DateOnly DataFutura => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14);

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

    private async Task<int> CriarServicoAsync(string token, bool agendavelOnline = true)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 350.00,
            duracaoEstimadaMinutos = 60,
            prazoMedioDias = 2,
            exigeDiagnostico = false,
            agendavelOnline,
            capacidadeSimultanea = 1,
            slaHoras = (int?)null,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<List<FilaEsperaResponse>> ListarFilaAsync(string token, string query = "") =>
        (await (await EnviarAsync(HttpMethod.Get, $"/api/fila-espera{query}", token))
            .Content.ReadFromJsonAsync<List<FilaEsperaResponse>>())!;

    [Fact]
    public async Task PortalEntraNaFilaEVinculaClientePorTelefone()
    {
        var (token, slug) = await RegistrarLojaAsync($"fila.{Guid.NewGuid():N}@exemplo.com");
        var servico = await CriarServicoAsync(token);

        var entrada = await _cliente.PostAsJsonAsync($"/api/publico/{slug}/fila-espera", new
        {
            servicoId = servico,
            nomeContato = "Cliente Portal",
            telefoneContato = "(11) 91234-5678",
            emailContato = (string?)null,
            dataPreferida = DataFutura.ToString("yyyy-MM-dd"),
            descricaoProblema = "Tela trincada",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });
        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);

        // Aparece na fila da loja e já nasceu vinculado a um cliente do CRM.
        var fila = await ListarFilaAsync(token);
        var item = Assert.Single(fila);
        Assert.Equal("Cliente Portal", item.NomeContato);
        Assert.Equal(OrigemAgendamento.Portal, item.Origem);
        Assert.Equal(StatusFilaEspera.Aguardando, item.Status);
        Assert.NotNull(item.ClienteId);

        // O vínculo silencioso criou/ligou um cliente de verdade.
        var clientes = await (await EnviarAsync(HttpMethod.Get, "/api/clientes", token))
            .Content.ReadFromJsonAsync<PaginaResponseDeCliente>();
        Assert.Contains(clientes!.Itens, c => c.Id == item.ClienteId);
    }

    [Fact]
    public async Task ConverterCriaAgendamentoEMarcaEntradaComoConvertida()
    {
        var (token, slug) = await RegistrarLojaAsync($"conv.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);

        await _cliente.PostAsJsonAsync($"/api/publico/{slug}/fila-espera", new
        {
            servicoId = servico,
            nomeContato = "Quer Vaga",
            telefoneContato = "(11) 90000-0000",
            emailContato = (string?)null,
            dataPreferida = (string?)null,
            descricaoProblema = "x",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });
        var entrada = (await ListarFilaAsync(token)).Single();

        var conv = await EnviarAsync(
            HttpMethod.Post, $"/api/fila-espera/{entrada.Id}/converter", token,
            new { data = DataFutura.ToString("yyyy-MM-dd"), horaInicio = "10:00:00" });
        Assert.Equal(HttpStatusCode.OK, conv.StatusCode);
        var convertida = (await conv.Content.ReadFromJsonAsync<FilaEsperaResponse>())!;
        Assert.Equal(StatusFilaEspera.Convertida, convertida.Status);
        Assert.NotNull(convertida.AgendamentoId);

        // O agendamento gerado existe e carrega os dados da entrada.
        var ag = await (await EnviarAsync(
                HttpMethod.Get, $"/api/agendamentos/{convertida.AgendamentoId}", token))
            .Content.ReadFromJsonAsync<AgendamentoResponse>();
        Assert.Equal("Quer Vaga", ag!.NomeContato);

        // Sai da lista ativa (só Aguardando por padrão) e não converte de novo.
        Assert.Empty(await ListarFilaAsync(token));
        var denovo = await EnviarAsync(
            HttpMethod.Post, $"/api/fila-espera/{entrada.Id}/converter", token,
            new { data = DataFutura.ToString("yyyy-MM-dd"), horaInicio = "11:00:00" });
        Assert.Equal(HttpStatusCode.BadRequest, denovo.StatusCode);
    }

    [Fact]
    public async Task ConverterEmHorarioIndisponivelMantemNaFila()
    {
        var (token, slug) = await RegistrarLojaAsync($"indisp.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token);
        await _cliente.PostAsJsonAsync($"/api/publico/{slug}/fila-espera", new
        {
            servicoId = servico,
            nomeContato = "Quer Vaga",
            telefoneContato = "(11) 90000-0000",
            emailContato = (string?)null,
            dataPreferida = (string?)null,
            descricaoProblema = "x",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });
        var entrada = (await ListarFilaAsync(token)).Single();

        // Fora do expediente (08:00 < abertura 09:00) → falha, sem consumir a entrada.
        var conv = await EnviarAsync(
            HttpMethod.Post, $"/api/fila-espera/{entrada.Id}/converter", token,
            new { data = DataFutura.ToString("yyyy-MM-dd"), horaInicio = "08:00:00" });
        Assert.Equal(HttpStatusCode.BadRequest, conv.StatusCode);
        Assert.Single(await ListarFilaAsync(token));
    }

    [Fact]
    public async Task DescartarTiraDaListaAtivaMasPreservaOHistorico()
    {
        var (token, _) = await RegistrarLojaAsync($"desc.{Guid.NewGuid():N}@exemplo.com");
        var servico = await CriarServicoAsync(token);
        var criada = await EnviarAsync(HttpMethod.Post, "/api/fila-espera", token, new
        {
            servicoId = servico,
            clienteId = (int?)null,
            nomeContato = "Ligou",
            telefoneContato = "(11) 98888-0000",
            emailContato = (string?)null,
            dataPreferida = (string?)null,
            descricaoProblema = (string?)null,
            aparelhoMarca = (string?)null,
            aparelhoModelo = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var entrada = (await criada.Content.ReadFromJsonAsync<FilaEsperaResponse>())!;
        Assert.Equal(OrigemAgendamento.Manual, entrada.Origem);

        var desc = await EnviarAsync(
            HttpMethod.Post, $"/api/fila-espera/{entrada.Id}/descartar", token,
            new { motivo = "Cliente desistiu" });
        Assert.Equal(HttpStatusCode.OK, desc.StatusCode);

        Assert.Empty(await ListarFilaAsync(token));
        var descartadas = await ListarFilaAsync(token, "?status=Descartada");
        Assert.Single(descartadas);
    }

    [Fact]
    public async Task FilaNaoAtravessaEmpresas()
    {
        var (tokenA, slugA) = await RegistrarLojaAsync($"filaA.{Guid.NewGuid():N}@exemplo.com");
        var (tokenB, _) = await RegistrarLojaAsync($"filaB.{Guid.NewGuid():N}@exemplo.com");
        var servicoA = await CriarServicoAsync(tokenA);
        await _cliente.PostAsJsonAsync($"/api/publico/{slugA}/fila-espera", new
        {
            servicoId = servicoA,
            nomeContato = "Da Loja A",
            telefoneContato = "(11) 90000-0000",
            emailContato = (string?)null,
            dataPreferida = (string?)null,
            descricaoProblema = "x",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });

        // B não vê a fila de A...
        Assert.Empty(await ListarFilaAsync(tokenB));
        var entradaDeA = (await ListarFilaAsync(tokenA)).Single();
        // ...nem consegue mexer nela.
        var descB = await EnviarAsync(
            HttpMethod.Post, $"/api/fila-espera/{entradaDeA.Id}/descartar", tokenB,
            new { motivo = "invasão" });
        Assert.Equal(HttpStatusCode.NotFound, descB.StatusCode);
    }

    private record PaginaResponseDeCliente(List<ClienteResponse> Itens, int Total);
}
