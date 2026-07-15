using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Comunicacao;
using TechPro.Api.Modules.Comunicacao.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Tests.Comunicacao;

/// <summary>
/// Notificações essenciais (módulo 9) no modo simulado (adaptador log, padrão
/// dos testes): disparo por evento, consentimento LGPD, lembrete e isolamento.
/// </summary>
public class ComunicacaoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly TechProApiFactory _fabrica = fabrica;
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private async Task<(string Token, Guid TenantId)> RegistrarEmpresaAsync(string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = $"Loja de {email}",
            nome = "Dono",
            email,
            senha = "senha123",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var auth = (await resposta.Content.ReadFromJsonAsync<AuthResponse>())!;
        return (auth.AccessToken, auth.Usuario.TenantId);
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

    private async Task<int> CriarClienteAsync(
        string token, bool comEmail = false, bool consentiu = true)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Carlos Cliente",
            telefone = "(11) 98888-7777",
            email = comEmail ? "carlos@exemplo.com" : "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = consentiu,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;
    }

    private async Task<int> CriarServicoAsync(string token, bool agendavelOnline = false)
    {
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 300m,
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
            aparelhoModelo = "Galaxy A54",
            descricaoProblema = "Tela trincada",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    private async Task<List<MensagemEnviadaResponse>> MensagensDaOsAsync(string token, Guid osId)
    {
        var resposta = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{osId}/mensagens", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<List<MensagemEnviadaResponse>>())!;
    }

    [Fact]
    public async Task OsCriadaNotificaNosCanaisDisponiveisEmModoSimulado()
    {
        var (token, _) = await RegistrarEmpresaAsync("com.oscriada@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token, comEmail: true), await CriarServicoAsync(token));

        var mensagens = await MensagensDaOsAsync(token, os.Id);
        // Todos os canais disponíveis: WhatsApp (telefone) + e-mail.
        Assert.Equal(2, mensagens.Count);
        Assert.All(mensagens, m => Assert.Equal(TipoEventoComunicacao.OrdemServicoCriada, m.TipoEvento));
        Assert.All(mensagens, m => Assert.Equal(StatusMensagem.Simulada, m.Status));
        Assert.Contains(mensagens, m => m.Canal == CanalNotificacao.WhatsApp);
        Assert.Contains(mensagens, m => m.Canal == CanalNotificacao.Email);
    }

    [Fact]
    public async Task SemEmailEnviaSoWhatsApp()
    {
        var (token, _) = await RegistrarEmpresaAsync("com.socanal@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token, comEmail: false), await CriarServicoAsync(token));

        var mensagem = Assert.Single(await MensagensDaOsAsync(token, os.Id));
        Assert.Equal(CanalNotificacao.WhatsApp, mensagem.Canal);
    }

    [Fact]
    public async Task ClienteSemConsentimentoTemMensagemSuprimida()
    {
        var (token, _) = await RegistrarEmpresaAsync("com.consentimento@exemplo.com");
        var os = await CriarOsAsync(
            token,
            await CriarClienteAsync(token, comEmail: true, consentiu: false),
            await CriarServicoAsync(token));

        var mensagens = await MensagensDaOsAsync(token, os.Id);
        Assert.Equal(2, mensagens.Count);
        // LGPD: registradas para auditoria, mas nunca enviadas.
        Assert.All(mensagens, m => Assert.Equal(StatusMensagem.Suprimida, m.Status));
    }

    [Fact]
    public async Task OrcamentoEProntoParaRetiradaGeramNotificacoes()
    {
        var (token, _) = await RegistrarEmpresaAsync("com.fluxo@exemplo.com");
        var os = await CriarOsAsync(
            token, await CriarClienteAsync(token), await CriarServicoAsync(token));

        await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}/orcamento",
            token, new { valorMaoDeObra = 200m, desconto = 0m });
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/enviar", token);
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/aprovar",
            token, new { motivo = (string?)null });
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/etapa",
            token, new { paraEtapa = "ProntoParaRetirada", motivo = (string?)null });

        var tipos = (await MensagensDaOsAsync(token, os.Id)).Select(m => m.TipoEvento).ToList();
        Assert.Contains(TipoEventoComunicacao.OrdemServicoCriada, tipos);
        Assert.Contains(TipoEventoComunicacao.OrcamentoDisponivel, tipos);
        Assert.Contains(TipoEventoComunicacao.OrcamentoAprovado, tipos);
        Assert.Contains(TipoEventoComunicacao.ProntoParaRetirada, tipos);
    }

    [Fact]
    public async Task AgendamentoConfirmadoNotificaELembreteRespeitaStatus()
    {
        var (token, tenantId) = await RegistrarEmpresaAsync("com.agendamento@exemplo.com");
        var servicoId = await CriarServicoAsync(token, agendavelOnline: true);

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

        var data = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(14);
        var agendar = await EnviarAsync(HttpMethod.Post, "/api/agendamentos", token, new
        {
            servicoId,
            data = data.ToString("yyyy-MM-dd"),
            horaInicio = "10:00:00",
            nomeContato = "Ana Agendada",
            telefoneContato = "(21) 97777-0000",
        });
        Assert.Equal(HttpStatusCode.Created, agendar.StatusCode);
        var agendamento = await agendar.Content
            .ReadFromJsonAsync<TechPro.Api.Modules.Agendamentos.Dtos.AgendamentoResponse>();
        var agId = agendamento!.Id;

        // Confirmação disparou no cadastro.
        Assert.Equal(1, await ContarMensagensAgendamentoAsync(
            tenantId, agId, TipoEventoComunicacao.AgendamentoConfirmado));

        // Lembrete (invocado direto — o job Hangfire chama este mesmo método):
        // agendamento ativo → envia.
        await ExecutarLembreteAsync(tenantId, agId);
        Assert.Equal(1, await ContarMensagensAgendamentoAsync(
            tenantId, agId, TipoEventoComunicacao.AgendamentoLembrete));

        // Cancelado → o lembrete não envia mais (guarda de status).
        var cancelar = await EnviarAsync(
            HttpMethod.Post, $"/api/agendamentos/{agId}/cancelar", token, new { motivo = "Teste" });
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        await ExecutarLembreteAsync(tenantId, agId);
        Assert.Equal(1, await ContarMensagensAgendamentoAsync(
            tenantId, agId, TipoEventoComunicacao.AgendamentoLembrete));
    }

    [Fact]
    public async Task MensagensNaoVazamEntreEmpresas()
    {
        var (tokenA, _) = await RegistrarEmpresaAsync("com.iso.a@exemplo.com");
        var (tokenB, _) = await RegistrarEmpresaAsync("com.iso.b@exemplo.com");
        var os = await CriarOsAsync(
            tokenA, await CriarClienteAsync(tokenA), await CriarServicoAsync(tokenA));

        // B consultando a OS da A não vê nenhuma mensagem (GQF).
        var mensagensB = await MensagensDaOsAsync(tokenB, os.Id);
        Assert.Empty(mensagensB);
        // A vê as suas.
        Assert.NotEmpty(await MensagensDaOsAsync(tokenA, os.Id));
    }

    // --- Auxiliares white-box para o lembrete (sem endpoint de agendamento) --------

    private async Task ExecutarLembreteAsync(Guid tenantId, int agendamentoId)
    {
        using var scope = _fabrica.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantAmbiente>().TenantIdFixado = tenantId;
        await scope.ServiceProvider.GetRequiredService<ComunicacaoService>()
            .NotificarLembreteAgendamentoAsync(agendamentoId);
    }

    private async Task<int> ContarMensagensAgendamentoAsync(
        Guid tenantId, int agendamentoId, TipoEventoComunicacao tipo)
    {
        using var scope = _fabrica.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantAmbiente>().TenantIdFixado = tenantId;
        var db = scope.ServiceProvider.GetRequiredService<TechProDbContext>();
        return await db.MensagensEnviadas
            .CountAsync(m => m.AgendamentoId == agendamentoId && m.TipoEvento == tipo);
    }
}
