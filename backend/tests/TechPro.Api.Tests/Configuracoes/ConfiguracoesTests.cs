using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Comunicacao;
using TechPro.Api.Modules.Comunicacao.Dtos;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Configuracoes;

/// <summary>
/// Configurações da loja, preferências de notificação (matriz evento × canal)
/// e conta do usuário (módulo 13).
/// </summary>
public class ConfiguracoesTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    [Fact]
    public async Task DadosDaLojaSalvamEAparecemNoPortalPublico()
    {
        var token = await RegistrarEmpresaAsync("cfg.loja@exemplo.com");

        var salvar = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/loja", token, new
        {
            nome = "AssisTech Centro",
            telefone = "(11) 3333-4444",
            email = "contato@assistech.com",
            endereco = "Rua das Flores, 100",
            politicas = "Garantia de 90 dias. Retirada em até 30 dias.",
        });
        Assert.Equal(HttpStatusCode.OK, salvar.StatusCode);
        var loja = await salvar.Content.ReadFromJsonAsync<LojaResponse>();
        Assert.Equal("AssisTech Centro", loja!.Nome);
        Assert.Equal("(11) 3333-4444", loja.Telefone);

        // Persistiu.
        var obter = await EnviarAsync(HttpMethod.Get, "/api/configuracoes/loja", token);
        var salva = await obter.Content.ReadFromJsonAsync<LojaResponse>();
        Assert.Equal("contato@assistech.com", salva!.Email);
        Assert.Equal("Rua das Flores, 100", salva.Endereco);
        Assert.Contains("Garantia de 90 dias", salva.Politicas);

        // O cliente final vê contato e políticas na página pública de agendamento.
        var publico = await _cliente.GetAsync($"/api/publico/{salva.Slug}/info");
        Assert.Equal(HttpStatusCode.OK, publico.StatusCode);
        var info = await publico.Content.ReadFromJsonAsync<LojaPublicaResponse>();
        Assert.Equal("AssisTech Centro", info!.Nome);
        Assert.Equal("(11) 3333-4444", info.Contato.Telefone);
        Assert.Contains("Garantia de 90 dias", info.Contato.Politicas);
    }

    [Fact]
    public async Task LojaValidaNomeEEmail()
    {
        var token = await RegistrarEmpresaAsync("cfg.loja.validacao@exemplo.com");

        var semNome = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/loja", token,
            new { nome = "", telefone = (string?)null, email = (string?)null, endereco = (string?)null, politicas = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, semNome.StatusCode);

        var emailInvalido = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/loja", token,
            new { nome = "Loja", telefone = (string?)null, email = "nao-e-email", endereco = (string?)null, politicas = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, emailInvalido.StatusCode);
    }

    [Fact]
    public async Task PreferenciasNascemTodasAtivasEPodemSerSalvas()
    {
        var token = await RegistrarEmpresaAsync("cfg.prefs@exemplo.com");

        var obter = await EnviarAsync(HttpMethod.Get, "/api/configuracoes/notificacoes", token);
        Assert.Equal(HttpStatusCode.OK, obter.StatusCode);
        var prefs = await obter.Content.ReadFromJsonAsync<PreferenciasNotificacaoResponse>();

        // Matriz completa: 8 eventos × 2 canais, tudo ativo por padrão (sem seed).
        Assert.Equal(16, prefs!.Itens.Count);
        Assert.All(prefs.Itens, i => Assert.True(i.Ativo));

        // Desliga só o lembrete por e-mail.
        var itens = prefs.Itens
            .Select(i => new
            {
                tipoEvento = i.TipoEvento.ToString(),
                canal = i.Canal.ToString(),
                ativo = !(i.TipoEvento == TipoEventoComunicacao.AgendamentoLembrete
                    && i.Canal == CanalNotificacao.Email),
            })
            .ToList();
        var salvar = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/notificacoes",
            token, new { itens });
        Assert.Equal(HttpStatusCode.OK, salvar.StatusCode);

        obter = await EnviarAsync(HttpMethod.Get, "/api/configuracoes/notificacoes", token);
        prefs = await obter.Content.ReadFromJsonAsync<PreferenciasNotificacaoResponse>();
        var lembreteEmail = Assert.Single(prefs!.Itens, i =>
            i.TipoEvento == TipoEventoComunicacao.AgendamentoLembrete
            && i.Canal == CanalNotificacao.Email);
        Assert.False(lembreteEmail.Ativo);
        // O mesmo evento por WhatsApp continua ligado (é matriz, não tudo-ou-nada).
        var lembreteWhats = Assert.Single(prefs.Itens, i =>
            i.TipoEvento == TipoEventoComunicacao.AgendamentoLembrete
            && i.Canal == CanalNotificacao.WhatsApp);
        Assert.True(lembreteWhats.Ativo);
    }

    [Fact]
    public async Task EventoDesligadoNaoEnviaMasFicaNaAuditoria()
    {
        var token = await RegistrarEmpresaAsync("cfg.gate@exemplo.com");

        // Desliga "OS criada" só no e-mail.
        var atuais = await (await EnviarAsync(HttpMethod.Get, "/api/configuracoes/notificacoes", token))
            .Content.ReadFromJsonAsync<PreferenciasNotificacaoResponse>();
        var itens = atuais!.Itens.Select(i => new
        {
            tipoEvento = i.TipoEvento.ToString(),
            canal = i.Canal.ToString(),
            ativo = !(i.TipoEvento == TipoEventoComunicacao.OrdemServicoCriada
                && i.Canal == CanalNotificacao.Email),
        }).ToList();
        await EnviarAsync(HttpMethod.Put, "/api/configuracoes/notificacoes", token, new { itens });

        // Cliente com e-mail + telefone: só o WhatsApp deve sair.
        var cliente = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Carlos",
            telefone = "(11) 98888-7777",
            email = "carlos@exemplo.com",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = true,
        });
        var clienteId = (await cliente.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;

        var servico = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 300m,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        });
        var servicoId = (await servico.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;

        var os = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId,
            servicoId,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "S23",
            descricaoProblema = "x",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        var ordem = (await os.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;

        var mensagens = await (await EnviarAsync(
                HttpMethod.Get, $"/api/ordens-servico/{ordem.Id}/mensagens", token))
            .Content.ReadFromJsonAsync<List<MensagemEnviadaResponse>>();

        // Os dois canais são registrados; o e-mail fica como Desativada.
        Assert.Equal(2, mensagens!.Count);
        var whats = Assert.Single(mensagens, m => m.Canal == CanalNotificacao.WhatsApp);
        Assert.Equal(StatusMensagem.Simulada, whats.Status);
        var mail = Assert.Single(mensagens, m => m.Canal == CanalNotificacao.Email);
        Assert.Equal(StatusMensagem.Desativada, mail.Status);
    }

    [Fact]
    public async Task ContaAtualizaNomeETrocaSenha()
    {
        const string email = "cfg.conta@exemplo.com";
        var token = await RegistrarEmpresaAsync(email);

        var renomear = await EnviarAsync(HttpMethod.Put, "/api/conta", token, new { nome = "Dono Renomeado" });
        Assert.Equal(HttpStatusCode.NoContent, renomear.StatusCode);
        var me = await (await EnviarAsync(HttpMethod.Get, "/api/auth/me", token))
            .Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal("Dono Renomeado", me!.Nome);

        // Senha atual errada → 400.
        var errada = await EnviarAsync(HttpMethod.Post, "/api/conta/senha", token,
            new { senhaAtual = "errada123", novaSenha = "novasenha123" });
        Assert.Equal(HttpStatusCode.BadRequest, errada.StatusCode);

        // Nova senha fraca → 400 de validação.
        var fraca = await EnviarAsync(HttpMethod.Post, "/api/conta/senha", token,
            new { senhaAtual = "senha123", novaSenha = "abc" });
        Assert.Equal(HttpStatusCode.BadRequest, fraca.StatusCode);

        // Troca válida: a senha antiga deixa de funcionar e a nova entra.
        var trocar = await EnviarAsync(HttpMethod.Post, "/api/conta/senha", token,
            new { senhaAtual = "senha123", novaSenha = "novasenha123" });
        Assert.Equal(HttpStatusCode.NoContent, trocar.StatusCode);

        var loginAntigo = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email, senha = "senha123" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginAntigo.StatusCode);

        var loginNovo = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email, senha = "novasenha123" });
        Assert.Equal(HttpStatusCode.OK, loginNovo.StatusCode);
    }

    [Fact]
    public async Task ConfiguracoesIsolamPorEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("cfg.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("cfg.iso.b@exemplo.com");

        await EnviarAsync(HttpMethod.Put, "/api/configuracoes/loja", tokenA, new
        {
            nome = "Loja A",
            telefone = "(11) 1111-1111",
            email = (string?)null,
            endereco = (string?)null,
            politicas = "Política da A",
        });

        var lojaB = await (await EnviarAsync(HttpMethod.Get, "/api/configuracoes/loja", tokenB))
            .Content.ReadFromJsonAsync<LojaResponse>();
        Assert.NotEqual("Loja A", lojaB!.Nome);
        Assert.Null(lojaB.Telefone);
        Assert.Null(lojaB.Politicas);
    }

    [Fact]
    public async Task ConfiguracoesExigemAutenticacao()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _cliente.GetAsync("/api/configuracoes/loja")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _cliente.GetAsync("/api/configuracoes/notificacoes")).StatusCode);
    }
}
