using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Agenda;

/// <summary>
/// Etapa "sinalização de peça em falta na agenda" (Fase 2): o serviço declara
/// peças padrão e a agenda avisa quando o estoque não cobre.
/// </summary>
public class PecasEmFaltaTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<int> CriarPecaAsync(string token, string nome, int estoque, bool ativo = true)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome,
            descricao = (string?)null,
            custoUnitario = 100.00,
            precoVenda = 200.00,
            quantidadeEmEstoque = estoque,
            estoqueMinimo = 1,
            fornecedorId = (int?)null,
            ativo,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PecaResponse>())!.Id;
    }

    private async Task<int> CriarServicoAsync(
        string token, string nome, params (int PecaId, int QuantidadePadrao)[] pecas)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome,
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
            pecas = pecas.Select(p => new { pecaId = p.PecaId, quantidadePadrao = p.QuantidadePadrao }),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<AgendamentoResponse> CriarAgendamentoAsync(
        string token, int servicoId, string hora)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId,
            data = DataFutura.ToString("yyyy-MM-dd"),
            horaInicio = hora,
            clienteId = (int?)null,
            nomeContato = "Carlos",
            telefoneContato = "(11) 98888-7777",
            emailContato = (string?)null,
            descricaoProblema = "x",
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<AgendamentoResponse>())!;
    }

    [Fact]
    public async Task ServicoComPecaAbaixoDoNecessarioSinalizaFalta()
    {
        var token = await RegistrarEmpresaAsync($"falta.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        // Precisa de 2 telas, só tem 1 → falta.
        var tela = await CriarPecaAsync(token, "Tela OLED", estoque: 1);
        var servico = await CriarServicoAsync(token, "Troca de tela dupla", (tela, 2));

        var ag = await CriarAgendamentoAsync(token, servico, "10:00:00");
        var falta = Assert.Single(ag.pecasEmFalta);
        Assert.Equal("Tela OLED", falta.PecaNome);
        Assert.Equal(2, falta.Necessario);
        Assert.Equal(1, falta.EmEstoque);
    }

    [Fact]
    public async Task ServicoAbastecidoNaoSinalizaNada()
    {
        var token = await RegistrarEmpresaAsync($"ok.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var tela = await CriarPecaAsync(token, "Tela", estoque: 10);
        var servico = await CriarServicoAsync(token, "Troca de tela", (tela, 1));

        var ag = await CriarAgendamentoAsync(token, servico, "10:00:00");
        Assert.Empty(ag.pecasEmFalta);
    }

    [Fact]
    public async Task ServicoSemPecaPadraoNuncaSinaliza()
    {
        var token = await RegistrarEmpresaAsync($"sempeca.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var servico = await CriarServicoAsync(token, "Diagnóstico");

        var ag = await CriarAgendamentoAsync(token, servico, "10:00:00");
        Assert.Empty(ag.pecasEmFalta);
    }

    [Fact]
    public async Task PecaInativaEIgnoradaNoSinal()
    {
        var token = await RegistrarEmpresaAsync($"inativa.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        // A peça está zerada, mas foi desativada — não faz sentido cobrar compra dela.
        var tela = await CriarPecaAsync(token, "Tela descontinuada", estoque: 0);
        var servico = await CriarServicoAsync(token, "Troca antiga", (tela, 1));
        await EnviarAsync(HttpMethod.Delete, $"/api/pecas/{tela}", token); // desativa

        var ag = await CriarAgendamentoAsync(token, servico, "10:00:00");
        Assert.Empty(ag.pecasEmFalta);
    }

    [Fact]
    public async Task ListagemCalculaFaltasEmLoteParaCadaServico()
    {
        var token = await RegistrarEmpresaAsync($"lote.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var telaCurta = await CriarPecaAsync(token, "Tela", estoque: 0);
        var bateriaOk = await CriarPecaAsync(token, "Bateria", estoque: 5);
        var servicoFalta = await CriarServicoAsync(token, "Troca de tela", (telaCurta, 1));
        var servicoOk = await CriarServicoAsync(token, "Troca de bateria", (bateriaOk, 1));

        var comFalta = await CriarAgendamentoAsync(token, servicoFalta, "09:00:00");
        var semFalta = await CriarAgendamentoAsync(token, servicoOk, "10:00:00");

        var lista = (await (await EnviarAsync(HttpMethod.Get, "/api/agendamentos", token))
            .Content.ReadFromJsonAsync<List<AgendamentoResponse>>())!;

        Assert.Single(lista.Single(a => a.Id == comFalta.Id).pecasEmFalta);
        Assert.Empty(lista.Single(a => a.Id == semFalta.Id).pecasEmFalta);
    }

    /// <summary>
    /// O sinal reflete o estoque atual: dar baixa em outra OS muda o que a
    /// agenda mostra (sem reserva — é o comportamento documentado).
    /// </summary>
    [Fact]
    public async Task SinalAcompanhaOEstoqueAtual()
    {
        var token = await RegistrarEmpresaAsync($"segue.{Guid.NewGuid():N}@exemplo.com");
        await ConfigurarSemanaAsync(token);
        var tela = await CriarPecaAsync(token, "Tela", estoque: 1);
        var servico = await CriarServicoAsync(token, "Troca de tela", (tela, 1));

        var ag = await CriarAgendamentoAsync(token, servico, "10:00:00");
        Assert.Empty(ag.pecasEmFalta); // 1 em estoque cobre a necessidade de 1

        // Consome a única unidade via movimentação de saída.
        await EnviarAsync(HttpMethod.Post, $"/api/pecas/{tela}/movimentacoes", token,
            new { tipo = "Saida", quantidade = 1, custoUnitario = (decimal?)null, motivo = "uso interno" });

        var recarregado = (await (await EnviarAsync(
                HttpMethod.Get, $"/api/agendamentos/{ag.Id}", token))
            .Content.ReadFromJsonAsync<AgendamentoResponse>())!;
        Assert.Single(recarregado.pecasEmFalta);
    }
}
