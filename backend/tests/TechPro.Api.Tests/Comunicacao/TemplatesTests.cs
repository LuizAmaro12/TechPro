using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Comunicacao;
using TechPro.Api.Modules.Comunicacao.Dtos;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Comunicacao;

/// <summary>
/// Etapa "templates editáveis e central de mensagens" (Fase 2): ausência =
/// padrão, personalização aplicada no envio, variável inválida barrada na
/// gravação e histórico unificado por cliente.
/// </summary>
public class TemplatesTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<TemplatesResponse> ObterTemplatesAsync(string token) =>
        (await (await EnviarAsync(HttpMethod.Get, "/api/configuracoes/templates", token))
            .Content.ReadFromJsonAsync<TemplatesResponse>())!;

    /// <summary>Cria uma OS com cliente que consentiu, para as mensagens saírem.</summary>
    private async Task<(OrdemServicoResponse Os, int ClienteId)> CriarOsAsync(string token)
    {
        var cliente = (await (await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria Souza",
            telefone = "(11) 90000-1234",
            email = "maria@exemplo.com",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = true,
        })).Content.ReadFromJsonAsync<ClienteResponse>())!;

        var servico = (await (await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Troca de tela",
            categoria = "Reparo",
            precoBase = 300.00,
            duracaoEstimadaMinutos = 60,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            slaHoras = (int?)null,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        })).Content.ReadFromJsonAsync<ServicoResponse>())!;

        var os = (await (await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId = cliente.Id,
            servicoId = servico.Id,
            aparelhoId = (int?)null,
            aparelhoMarca = "Samsung",
            aparelhoModelo = "Galaxy S23",
            descricaoProblema = "Tela trincada",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        })).Content.ReadFromJsonAsync<OrdemServicoResponse>())!;

        return (os, cliente.Id);
    }

    private async Task<List<MensagemEnviadaResponse>> MensagensDoClienteAsync(
        string token, int clienteId) =>
        (await (await EnviarAsync(HttpMethod.Get, $"/api/clientes/{clienteId}/mensagens", token))
            .Content.ReadFromJsonAsync<List<MensagemEnviadaResponse>>())!;

    [Fact]
    public async Task TemplatesNascemNoPadraoSemPersonalizacao()
    {
        var token = await RegistrarEmpresaAsync($"tpl.{Guid.NewGuid():N}@exemplo.com");
        var templates = await ObterTemplatesAsync(token);

        // Um item por evento, todos vindos do padrão (sem seed no banco).
        Assert.Equal(8, templates.Itens.Count);
        Assert.All(templates.Itens, i => Assert.False(i.Personalizado));

        var pronto = templates.Itens.Single(
            i => i.TipoEvento == TipoEventoComunicacao.ProntoParaRetirada);
        Assert.Contains("{cliente}", pronto.Corpo);
        Assert.Contains("aparelho", pronto.VariaveisDisponiveis);
    }

    [Fact]
    public async Task PersonalizacaoEUsadaNoEnvioEVoltarAoPadraoFunciona()
    {
        var token = await RegistrarEmpresaAsync($"tpluso.{Guid.NewGuid():N}@exemplo.com");

        var salvar = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/templates", token, new
        {
            itens = new[]
            {
                new
                {
                    tipoEvento = "OrdemServicoCriada",
                    assunto = "Chegou na {loja}!",
                    corpo = "Oi {cliente}, seu {aparelho} entrou como OS {numero}. Segue: {link}",
                },
            },
        });
        Assert.Equal(HttpStatusCode.OK, salvar.StatusCode);
        var apos = (await salvar.Content.ReadFromJsonAsync<TemplatesResponse>())!;
        Assert.True(apos.Itens.Single(
            i => i.TipoEvento == TipoEventoComunicacao.OrdemServicoCriada).Personalizado);

        // A OS criada dispara a mensagem já com o texto personalizado renderizado.
        var (_, clienteId) = await CriarOsAsync(token);
        var mensagens = await MensagensDoClienteAsync(token, clienteId);
        Assert.NotEmpty(mensagens);
        Assert.All(mensagens, m =>
            Assert.Equal(TipoEventoComunicacao.OrdemServicoCriada, m.TipoEvento));

        // Corpo vazio remove a personalização (volta ao padrão).
        var voltar = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/templates", token, new
        {
            itens = new[]
            {
                new { tipoEvento = "OrdemServicoCriada", assunto = (string?)null, corpo = "  " },
            },
        });
        Assert.Equal(HttpStatusCode.OK, voltar.StatusCode);
        var depois = (await voltar.Content.ReadFromJsonAsync<TemplatesResponse>())!;
        var item = depois.Itens.Single(i => i.TipoEvento == TipoEventoComunicacao.OrdemServicoCriada);
        Assert.False(item.Personalizado);
        Assert.Contains("Acompanhe por aqui", item.Corpo);
    }

    /// <summary>
    /// O erro de digitação é pego na configuração — nunca vira texto quebrado
    /// na mensagem do cliente.
    /// </summary>
    [Fact]
    public async Task VariavelInexistenteParaOEventoEhRejeitada()
    {
        var token = await RegistrarEmpresaAsync($"tplvar.{Guid.NewGuid():N}@exemplo.com");

        // {valor} não existe em ProntoParaRetirada.
        var resposta = await EnviarAsync(HttpMethod.Put, "/api/configuracoes/templates", token, new
        {
            itens = new[]
            {
                new
                {
                    tipoEvento = "ProntoParaRetirada",
                    assunto = (string?)null,
                    corpo = "Seu aparelho custou {valor}",
                },
            },
        });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("{valor}", corpo);

        // Nada foi salvo: o evento continua no padrão.
        var templates = await ObterTemplatesAsync(token);
        Assert.False(templates.Itens.Single(
            i => i.TipoEvento == TipoEventoComunicacao.ProntoParaRetirada).Personalizado);
    }

    [Fact]
    public async Task CentralDeMensagensAgrupaPorClienteEMostraSuprimidas()
    {
        var token = await RegistrarEmpresaAsync($"central.{Guid.NewGuid():N}@exemplo.com");

        // Cliente SEM consentimento: a mensagem é registrada como Suprimida.
        var semConsentimento = (await (await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Sem Consentimento",
            telefone = "(11) 95555-0001",
            email = "sem@exemplo.com",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        })).Content.ReadFromJsonAsync<ClienteResponse>())!;

        var servico = (await (await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Limpeza",
            categoria = "Reparo",
            precoBase = 80.00,
            duracaoEstimadaMinutos = 30,
            prazoMedioDias = (int?)null,
            exigeDiagnostico = false,
            agendavelOnline = false,
            capacidadeSimultanea = 1,
            slaHoras = (int?)null,
            ativo = true,
            checklist = Array.Empty<string>(),
            pecas = Array.Empty<object>(),
        })).Content.ReadFromJsonAsync<ServicoResponse>())!;

        await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId = semConsentimento.Id,
            servicoId = servico.Id,
            aparelhoId = (int?)null,
            aparelhoMarca = "Xiaomi",
            aparelhoModelo = "Note 12",
            descricaoProblema = "x",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });

        var mensagens = await MensagensDoClienteAsync(token, semConsentimento.Id);
        Assert.NotEmpty(mensagens);
        // É justamente o que responde "por que meu cliente não recebeu?".
        Assert.All(mensagens, m => Assert.Equal(StatusMensagem.Suprimida, m.Status));

        // Outro cliente não vê as mensagens deste.
        var (_, outroClienteId) = await CriarOsAsync(token);
        Assert.DoesNotContain(
            await MensagensDoClienteAsync(token, outroClienteId),
            m => mensagens.Select(x => x.Id).Contains(m.Id));
    }

    [Fact]
    public async Task TemplatesENaoAtravessamEmpresas()
    {
        var tokenA = await RegistrarEmpresaAsync($"tplA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"tplB.{Guid.NewGuid():N}@exemplo.com");

        await EnviarAsync(HttpMethod.Put, "/api/configuracoes/templates", tokenA, new
        {
            itens = new[]
            {
                new
                {
                    tipoEvento = "ProntoParaRetirada",
                    assunto = (string?)null,
                    corpo = "Texto exclusivo da loja A para {cliente}",
                },
            },
        });

        // Para B, o evento continua no padrão.
        var deB = await ObterTemplatesAsync(tokenB);
        var item = deB.Itens.Single(i => i.TipoEvento == TipoEventoComunicacao.ProntoParaRetirada);
        Assert.False(item.Personalizado);
        Assert.DoesNotContain("exclusivo da loja A", item.Corpo);
    }
}
