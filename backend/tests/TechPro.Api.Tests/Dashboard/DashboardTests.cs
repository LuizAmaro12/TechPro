using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Dashboard.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Dashboard;

public class DashboardTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<int> CriarClienteAsync(string token, string nome = "Cliente Dash")
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome,
            telefone = "(11) 98888-7777",
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
            precoBase = 300m,
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

    private async Task<OrdemServicoResponse> CriarOsAsync(
        string token, int clienteId, int servicoId, string? prazo = null)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
            descricaoProblema = "Tela",
            prioridade = "Normal",
            prazoEstimado = prazo,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task MoverEtapaAsync(string token, Guid osId, string etapa) =>
        Assert.Equal(HttpStatusCode.OK,
            (await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/etapa",
                token, new { paraEtapa = etapa, motivo = etapa == "Cancelado" ? "x" : null }))
            .StatusCode);

    private async Task<DashboardResponse> DashboardAsync(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Get, "/api/dashboard", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<DashboardResponse>())!;
    }

    [Fact]
    public async Task ContadoresRefletemOFluxoDaOficina()
    {
        var token = await RegistrarEmpresaAsync("dash.contadores@exemplo.com");
        var clienteId = await CriarClienteAsync(token);
        var servicoId = await CriarServicoAsync(token);

        // 3 OS: uma em reparo, uma pronta, uma entregue (finalizada).
        var emReparo = await CriarOsAsync(token, clienteId, servicoId);
        await MoverEtapaAsync(token, emReparo.Id, "EmReparo");

        var pronta = await CriarOsAsync(token, clienteId, servicoId);
        await MoverEtapaAsync(token, pronta.Id, "ProntoParaRetirada");

        var entregue = await CriarOsAsync(token, clienteId, servicoId);
        await MoverEtapaAsync(token, entregue.Id, "ProntoParaRetirada");
        await MoverEtapaAsync(token, entregue.Id, "Entregue");

        var dash = await DashboardAsync(token);
        Assert.Equal(2, dash.OsAbertas);          // reparo + pronta (entregue não conta)
        Assert.Equal(1, dash.AparelhosEmReparo);  // bancada: só a em reparo
        Assert.Equal(1, dash.ProntosParaRetirada);
        Assert.Equal(0, dash.ServicosEmAtraso);   // nenhuma tem prazo
    }

    [Fact]
    public async Task ServicoEmAtrasoERadarListamOsComPrazoVencido()
    {
        var token = await RegistrarEmpresaAsync("dash.atraso@exemplo.com");
        var clienteId = await CriarClienteAsync(token, "Maria Atrasada");
        var servicoId = await CriarServicoAsync(token);

        var ontem = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-3);
        var atrasada = await CriarOsAsync(token, clienteId, servicoId, prazo: ontem.ToString("yyyy-MM-dd"));
        // No prazo (futuro): não conta.
        await CriarOsAsync(token, clienteId, servicoId,
            prazo: DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(5).ToString("yyyy-MM-dd"));

        var dash = await DashboardAsync(token);
        Assert.Equal(1, dash.ServicosEmAtraso);
        var item = Assert.Single(dash.Radar.OsAtrasadas);
        Assert.Equal(atrasada.Numero, item.Numero);
        Assert.Equal("Maria Atrasada", item.ClienteNome);
        Assert.Equal(3, item.DiasAtraso);

        // OS finalizada sai do atraso.
        await MoverEtapaAsync(token, atrasada.Id, "Cancelado");
        dash = await DashboardAsync(token);
        Assert.Equal(0, dash.ServicosEmAtraso);
        Assert.Empty(dash.Radar.OsAtrasadas);
    }

    [Fact]
    public async Task FaturamentoSomaOsPagamentosDoMes()
    {
        var token = await RegistrarEmpresaAsync("dash.faturamento@exemplo.com");
        var os = await CriarOsAsync(token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        // Orçamento + dois pagamentos no mês corrente.
        await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}/orcamento",
            token, new { valorMaoDeObra = 300m, desconto = 0m });
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/enviar", token);
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pagamentos",
            token, new { valor = 100m, forma = "Pix", observacao = (string?)null });
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pagamentos",
            token, new { valor = 50m, forma = "Dinheiro", observacao = (string?)null });

        var dash = await DashboardAsync(token);
        Assert.Equal(150m, dash.FaturamentoMes);
        Assert.Equal(0m, dash.FaturamentoMesAnterior);
        Assert.Null(dash.VariacaoFaturamentoPct); // mês anterior zero → sem %
    }

    [Fact]
    public async Task AgendamentosDoDiaERadarDeOrcamentoPendente()
    {
        var token = await RegistrarEmpresaAsync("dash.agenda@exemplo.com");
        var servicoId = await CriarServicoAsync(token);

        var horarios = await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new
        {
            dias = Enumerable.Range(0, 7).Select(d => new
            {
                diaSemana = d,
                ativo = true,
                abertura = "08:00:00",
                fechamento = "20:00:00",
                intervaloInicio = (string?)null,
                intervaloFim = (string?)null,
            }).ToList(),
        });
        Assert.Equal(HttpStatusCode.OK, horarios.StatusCode);

        // Agendamento para hoje.
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var agendar = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId,
            data = hoje.ToString("yyyy-MM-dd"),
            horaInicio = "10:00:00",
            nomeContato = "Ana Hoje",
            telefoneContato = "(21) 97777-0000",
        });
        Assert.Equal(HttpStatusCode.Created, agendar.StatusCode);

        var dash = await DashboardAsync(token);
        Assert.Equal(1, dash.AgendamentosHoje);
        // Orçamento recém-enviado ainda não é "pendente há mais de 2 dias".
        Assert.Empty(dash.Radar.OrcamentosPendentes);
    }

    [Fact]
    public async Task DashboardIsolaPorEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("dash.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("dash.iso.b@exemplo.com");
        var os = await CriarOsAsync(tokenA, await CriarClienteAsync(tokenA), await CriarServicoAsync(tokenA));
        await MoverEtapaAsync(tokenA, os.Id, "EmReparo");

        // B não enxerga nada da A.
        var dashB = await DashboardAsync(tokenB);
        Assert.Equal(0, dashB.OsAbertas);
        Assert.Equal(0, dashB.AparelhosEmReparo);

        var dashA = await DashboardAsync(tokenA);
        Assert.Equal(1, dashA.AparelhosEmReparo);
    }

    [Fact]
    public async Task DashboardExigeAutenticacao()
    {
        var resposta = await _cliente.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
