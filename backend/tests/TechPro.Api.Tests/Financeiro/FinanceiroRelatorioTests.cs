using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Financeiro;

/// <summary>
/// Relatório de caixa (módulo 8): faturamento por período, transações,
/// a receber (só aprovados), ticket médio e projeção.
/// </summary>
public class FinanceiroRelatorioTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<int> CriarClienteAsync(string token, string nome)
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

    private async Task<int> CriarServicoAsync(string token, decimal precoBase = 300m)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase,
            duracaoEstimadaMinutos = 60,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = true,
            capacidadeSimultanea = 2,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;
    }

    private async Task<OrdemServicoResponse> CriarOsAsync(string token, int clienteId, int servicoId)
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
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    /// <summary>Cria orçamento, envia e (opcionalmente) aprova.</summary>
    private async Task OrcarAsync(string token, Guid osId, decimal maoDeObra, bool aprovar)
    {
        await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{osId}/orcamento",
            token, new { valorMaoDeObra = maoDeObra, desconto = 0m });
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/orcamento/enviar", token);
        if (aprovar)
        {
            await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/orcamento/aprovar",
                token, new { motivo = (string?)null });
        }
    }

    private async Task PagarAsync(string token, Guid osId, decimal valor, string forma = "Pix") =>
        Assert.Equal(HttpStatusCode.Created,
            (await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{osId}/pagamentos",
                token, new { valor, forma, observacao = (string?)null })).StatusCode);

    private async Task<FinanceiroRelatorioResponse> RelatorioAsync(string token, string query = "")
    {
        var resposta = await EnviarAsync(HttpMethod.Get, $"/api/financeiro{query}", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<FinanceiroRelatorioResponse>())!;
    }

    [Fact]
    public async Task FaturamentoTicketMedioETransacoesDoPeriodo()
    {
        var token = await RegistrarEmpresaAsync("fin.faturamento@exemplo.com");
        var servicoId = await CriarServicoAsync(token);

        // OS 1: duas transações (100 + 50).
        var os1 = await CriarOsAsync(token, await CriarClienteAsync(token, "Ana"), servicoId);
        await OrcarAsync(token, os1.Id, 300m, aprovar: true);
        await PagarAsync(token, os1.Id, 100m, "Pix");
        await PagarAsync(token, os1.Id, 50m, "Dinheiro");

        // OS 2: uma transação (250).
        var os2 = await CriarOsAsync(token, await CriarClienteAsync(token, "Bruno"), servicoId);
        await OrcarAsync(token, os2.Id, 250m, aprovar: true);
        await PagarAsync(token, os2.Id, 250m, "Pix");

        var relatorio = await RelatorioAsync(token);
        Assert.Equal(400m, relatorio.Faturamento);       // 100 + 50 + 250
        Assert.Equal(3, relatorio.QuantidadeTransacoes);
        Assert.Equal(2, relatorio.QuantidadeOsPagas);    // duas OS distintas
        Assert.Equal(200m, relatorio.TicketMedio);       // 400 / 2

        // Composição por forma: Pix 350 (2), Dinheiro 50 (1).
        var pix = Assert.Single(relatorio.PorForma, f => f.Forma == FormaPagamento.Pix);
        Assert.Equal(350m, pix.Total);
        Assert.Equal(2, pix.Quantidade);
        var dinheiro = Assert.Single(relatorio.PorForma, f => f.Forma == FormaPagamento.Dinheiro);
        Assert.Equal(50m, dinheiro.Total);

        // As transações trazem OS e cliente.
        Assert.Contains(relatorio.Transacoes, t => t.ClienteNome == "Ana" && t.Valor == 100m);
        Assert.Contains(relatorio.Transacoes, t => t.ClienteNome == "Bruno" && t.Valor == 250m);
    }

    [Fact]
    public async Task AReceberContaSoOrcamentoAprovadoComSaldo()
    {
        var token = await RegistrarEmpresaAsync("fin.areceber@exemplo.com");
        var servicoId = await CriarServicoAsync(token);

        // Aprovado com saldo: 300 − 100 = 200 a receber.
        var comSaldo = await CriarOsAsync(token, await CriarClienteAsync(token, "Com Saldo"), servicoId);
        await OrcarAsync(token, comSaldo.Id, 300m, aprovar: true);
        await PagarAsync(token, comSaldo.Id, 100m);

        // Aprovado e quitado: não entra.
        var quitada = await CriarOsAsync(token, await CriarClienteAsync(token, "Quitada"), servicoId);
        await OrcarAsync(token, quitada.Id, 150m, aprovar: true);
        await PagarAsync(token, quitada.Id, 150m);

        // Enviado SEM resposta: é proposta, não receita vendida — não entra.
        var semResposta = await CriarOsAsync(token, await CriarClienteAsync(token, "Sem Resposta"), servicoId);
        await OrcarAsync(token, semResposta.Id, 999m, aprovar: false);

        var relatorio = await RelatorioAsync(token);
        Assert.Equal(200m, relatorio.AReceber);
        var pendente = Assert.Single(relatorio.Pendentes);
        Assert.Equal("Com Saldo", pendente.ClienteNome);
        Assert.Equal(300m, pendente.Total);
        Assert.Equal(100m, pendente.Pago);
        Assert.Equal(200m, pendente.Saldo);
    }

    [Fact]
    public async Task OsCanceladaSaiDoAReceber()
    {
        var token = await RegistrarEmpresaAsync("fin.cancelada@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token, "Cancelada"), await CriarServicoAsync(token));
        await OrcarAsync(token, os.Id, 300m, aprovar: true);
        Assert.Equal(300m, (await RelatorioAsync(token)).AReceber);

        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "Cancelado", motivo = "Desistiu" });

        var relatorio = await RelatorioAsync(token);
        Assert.Equal(0m, relatorio.AReceber);
        Assert.Empty(relatorio.Pendentes);
    }

    [Fact]
    public async Task ProjecaoSomaAReceberComAgendamentosDosProximosDias()
    {
        var token = await RegistrarEmpresaAsync("fin.projecao@exemplo.com");
        var servicoId = await CriarServicoAsync(token, precoBase: 300m);

        // 200 a receber (aprovado, pago parcial).
        var os = await CriarOsAsync(token, await CriarClienteAsync(token, "Ana"), servicoId);
        await OrcarAsync(token, os.Id, 300m, aprovar: true);
        await PagarAsync(token, os.Id, 100m);

        // Um agendamento em 3 dias → soma o preço base (300).
        await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new
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
        var data = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(3);
        Assert.Equal(HttpStatusCode.Created,
            (await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
            {
                servicoId,
                data = data.ToString("yyyy-MM-dd"),
                horaInicio = "10:00:00",
                nomeContato = "Futuro",
                telefoneContato = "(21) 97777-0000",
            })).StatusCode);

        var relatorio = await RelatorioAsync(token);
        Assert.Equal(200m, relatorio.Projecao.AprovadosAReceber);
        Assert.Equal(300m, relatorio.Projecao.AgendamentosProximos7Dias);
        Assert.Equal(500m, relatorio.Projecao.Total);
    }

    [Fact]
    public async Task PeriodoFiltraOFaturamentoEValidaAsDatas()
    {
        var token = await RegistrarEmpresaAsync("fin.periodo@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token, "Ana"), await CriarServicoAsync(token));
        await OrcarAsync(token, os.Id, 300m, aprovar: true);
        await PagarAsync(token, os.Id, 100m);

        // Período que não contém hoje: sem faturamento.
        var passado = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-30);
        var relatorio = await RelatorioAsync(token,
            $"?de={passado:yyyy-MM-dd}&ate={passado.AddDays(5):yyyy-MM-dd}");
        Assert.Equal(0m, relatorio.Faturamento);
        Assert.Empty(relatorio.Transacoes);
        Assert.Equal(0m, relatorio.TicketMedio);
        // "A receber" e projeção não dependem do período (são visão atual).
        Assert.Equal(200m, relatorio.AReceber);

        // Hoje incluso: pega o pagamento.
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        relatorio = await RelatorioAsync(token, $"?de={hoje:yyyy-MM-dd}&ate={hoje:yyyy-MM-dd}");
        Assert.Equal(100m, relatorio.Faturamento);

        // Data final antes da inicial → 400.
        var invalido = await EnviarAsync(HttpMethod.Get,
            $"/api/financeiro?de={hoje:yyyy-MM-dd}&ate={hoje.AddDays(-1):yyyy-MM-dd}", token);
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task RelatorioIsolaPorEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("fin.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("fin.iso.b@exemplo.com");
        var os = await CriarOsAsync(
            tokenA, await CriarClienteAsync(tokenA, "Ana"), await CriarServicoAsync(tokenA));
        await OrcarAsync(tokenA, os.Id, 300m, aprovar: true);
        await PagarAsync(tokenA, os.Id, 300m);

        var relatorioB = await RelatorioAsync(tokenB);
        Assert.Equal(0m, relatorioB.Faturamento);
        Assert.Empty(relatorioB.Transacoes);
        Assert.Equal(0m, relatorioB.AReceber);

        Assert.Equal(300m, (await RelatorioAsync(tokenA)).Faturamento);
    }

    [Fact]
    public async Task RelatorioExigeAutenticacao()
    {
        var resposta = await _cliente.GetAsync("/api/financeiro");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
