using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Auth;

/// <summary>
/// Etapa "equipe, permissões e histórico de ações" (Fase 2). Antes desta etapa
/// não havia como adicionar um segundo usuário e **nenhum** endpoint checava
/// papel — qualquer autenticado podia tudo.
/// </summary>
public class EquipePermissoesTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private async Task<string> RegistrarGestorAsync(string email)
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
        HttpMethod metodo, string url, string? token, object? corpo = null)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        if (token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return await _cliente.SendAsync(requisicao);
    }

    /// <summary>Cria o membro pelo gestor e devolve o token dele já logado.</summary>
    private async Task<(Guid Id, string Token)> CriarMembroAsync(
        string tokenGestor, string papel, string email)
    {
        var criado = await EnviarAsync(HttpMethod.Post, "/api/equipe", tokenGestor, new
        {
            nome = $"Membro {papel}",
            email,
            senha = "senha123",
            papel,
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var membro = (await criado.Content.ReadFromJsonAsync<EquipeMembroResponse>())!;
        Assert.Equal(papel, membro.Papel);

        var login = await _cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = "senha123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        return (membro.Id, token);
    }

    [Fact]
    public async Task GestorAdicionaMembrosComFuncaoEElesConseguemEntrar()
    {
        var gestor = await RegistrarGestorAsync($"eq.{Guid.NewGuid():N}@exemplo.com");
        var (_, tokenTecnico) = await CriarMembroAsync(
            gestor, Papeis.Tecnico, $"tec.{Guid.NewGuid():N}@exemplo.com");

        // O membro entra e enxerga a operação (Kanban/OS).
        var os = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", tokenTecnico);
        Assert.Equal(HttpStatusCode.OK, os.StatusCode);

        var equipe = (await (await EnviarAsync(HttpMethod.Get, "/api/equipe", gestor))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!;
        Assert.Equal(2, equipe.Count);
        Assert.Contains(equipe, m => m.Papel == Papeis.Tecnico);
    }

    /// <summary>Técnico opera a bancada, mas não vê dinheiro nem configurações.</summary>
    [Fact]
    public async Task TecnicoNaoAcessaFinanceiroConfiguracoesNemLgpd()
    {
        var gestor = await RegistrarGestorAsync($"eqt.{Guid.NewGuid():N}@exemplo.com");
        var (_, tecnico) = await CriarMembroAsync(
            gestor, Papeis.Tecnico, $"tec2.{Guid.NewGuid():N}@exemplo.com");

        foreach (var rota in new[]
                 {
                     "/api/financeiro/rentabilidade",
                     "/api/configuracoes/loja",
                     "/api/configuracoes/templates",
                     "/api/auditoria",
                 })
        {
            var resposta = await EnviarAsync(HttpMethod.Get, rota, tecnico);
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        // Mas a bancada é dele: peças e estoque respondem.
        Assert.Equal(HttpStatusCode.OK,
            (await EnviarAsync(HttpMethod.Get, "/api/pecas", tecnico)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EnviarAsync(HttpMethod.Get, "/api/estoque/lista-compra", tecnico)).StatusCode);
    }

    /// <summary>Atendente cuida do balcão, mas não vê custo de peça.</summary>
    [Fact]
    public async Task AtendenteNaoAcessaPecasNemEstoqueMasAtendeClientes()
    {
        var gestor = await RegistrarGestorAsync($"eqa.{Guid.NewGuid():N}@exemplo.com");
        var (_, atendente) = await CriarMembroAsync(
            gestor, Papeis.Atendente, $"aten.{Guid.NewGuid():N}@exemplo.com");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await EnviarAsync(HttpMethod.Get, "/api/pecas", atendente)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await EnviarAsync(HttpMethod.Get, "/api/estoque/lista-compra", atendente)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await EnviarAsync(HttpMethod.Get, "/api/financeiro/rentabilidade", atendente)).StatusCode);

        // Cadastrar cliente é o trabalho dele.
        var criado = await EnviarAsync(HttpMethod.Post, "/api/clientes", atendente, new
        {
            nome = "Cliente do balcão",
            telefone = "(11) 90000-7777",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
    }

    /// <summary>Técnico só vê a agenda e os clientes — não escreve.</summary>
    [Fact]
    public async Task TecnicoLeAgendaEClientesMasNaoEscreve()
    {
        var gestor = await RegistrarGestorAsync($"eqr.{Guid.NewGuid():N}@exemplo.com");
        var (_, tecnico) = await CriarMembroAsync(
            gestor, Papeis.Tecnico, $"tec3.{Guid.NewGuid():N}@exemplo.com");

        Assert.Equal(HttpStatusCode.OK,
            (await EnviarAsync(HttpMethod.Get, "/api/clientes", tecnico)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EnviarAsync(HttpMethod.Get, "/api/agendamentos", tecnico)).StatusCode);

        var tentativa = await EnviarAsync(HttpMethod.Post, "/api/clientes", tecnico, new
        {
            nome = "Não deveria entrar",
            telefone = "(11) 90000-8888",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });
        Assert.Equal(HttpStatusCode.Forbidden, tentativa.StatusCode);
    }

    [Fact]
    public async Task SoOGestorAdministraAEquipe()
    {
        var gestor = await RegistrarGestorAsync($"eqg.{Guid.NewGuid():N}@exemplo.com");
        var (_, atendente) = await CriarMembroAsync(
            gestor, Papeis.Atendente, $"aten2.{Guid.NewGuid():N}@exemplo.com");

        var tentativa = await EnviarAsync(HttpMethod.Post, "/api/equipe", atendente, new
        {
            nome = "Intruso",
            email = $"intruso.{Guid.NewGuid():N}@exemplo.com",
            senha = "senha123",
            papel = Papeis.Gestor,
        });
        Assert.Equal(HttpStatusCode.Forbidden, tentativa.StatusCode);
    }

    /// <summary>
    /// Sem esta guarda a loja se tranca para fora das configurações, do
    /// financeiro e da própria equipe — sem caminho de volta.
    /// </summary>
    [Fact]
    public async Task NaoDaParaRebaixarNemDesativarOUnicoGestor()
    {
        var gestor = await RegistrarGestorAsync($"eqx.{Guid.NewGuid():N}@exemplo.com");
        var equipe = (await (await EnviarAsync(HttpMethod.Get, "/api/equipe", gestor))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!;
        var dono = equipe.Single();

        var rebaixar = await EnviarAsync(HttpMethod.Put, $"/api/equipe/{dono.Id}", gestor,
            new { nome = dono.Nome, papel = Papeis.Tecnico });
        Assert.Equal(HttpStatusCode.BadRequest, rebaixar.StatusCode);

        var desativar = await EnviarAsync(HttpMethod.Delete, $"/api/equipe/{dono.Id}", gestor);
        Assert.Equal(HttpStatusCode.BadRequest, desativar.StatusCode);

        // Com um segundo gestor, o rebaixamento passa a ser permitido.
        await CriarMembroAsync(gestor, Papeis.Gestor, $"gestor2.{Guid.NewGuid():N}@exemplo.com");
        var agora = await EnviarAsync(HttpMethod.Put, $"/api/equipe/{dono.Id}", gestor,
            new { nome = dono.Nome, papel = Papeis.Tecnico });
        Assert.Equal(HttpStatusCode.OK, agora.StatusCode);
    }

    [Fact]
    public async Task MembroDesativadoNaoConsegueMaisEntrar()
    {
        var gestor = await RegistrarGestorAsync($"eqd.{Guid.NewGuid():N}@exemplo.com");
        var emailTecnico = $"tec4.{Guid.NewGuid():N}@exemplo.com";
        var (id, _) = await CriarMembroAsync(gestor, Papeis.Tecnico, emailTecnico);

        var desativar = await EnviarAsync(HttpMethod.Delete, $"/api/equipe/{id}", gestor);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var login = await _cliente.PostAsJsonAsync(
            "/api/auth/login", new { email = emailTecnico, senha = "senha123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        // Some da listagem padrão, mas continua visível com incluirInativos.
        var ativos = (await (await EnviarAsync(HttpMethod.Get, "/api/equipe", gestor))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!;
        Assert.DoesNotContain(ativos, m => m.Id == id);
        var todos = (await (await EnviarAsync(
                HttpMethod.Get, "/api/equipe?incluirInativos=true", gestor))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!;
        Assert.Contains(todos, m => m.Id == id && !m.Ativo);
    }

    [Fact]
    public async Task AcoesSensiveisEntramNoHistorico()
    {
        var gestor = await RegistrarGestorAsync($"eqh.{Guid.NewGuid():N}@exemplo.com");
        await CriarMembroAsync(gestor, Papeis.Tecnico, $"tec5.{Guid.NewGuid():N}@exemplo.com");

        // Alterar configuração também é auditado.
        await EnviarAsync(HttpMethod.Put, "/api/configuracoes/loja", gestor, new
        {
            nome = "Loja Renomeada",
            telefone = "(11) 3333-0000",
            email = "loja@exemplo.com",
            endereco = "Rua A, 10",
            politicas = "90 dias de garantia",
        });

        var registros = (await (await EnviarAsync(HttpMethod.Get, "/api/auditoria", gestor))
            .Content.ReadFromJsonAsync<List<RegistroAuditoriaResponse>>())!;
        Assert.Contains(registros, r => r.Acao == "Membro adicionado" && r.UsuarioNome == "Dono");
        Assert.Contains(registros, r => r.Entidade == "Configurações");

        // Filtro por área funciona.
        var soEquipe = (await (await EnviarAsync(
                HttpMethod.Get, "/api/auditoria?entidade=Equipe", gestor))
            .Content.ReadFromJsonAsync<List<RegistroAuditoriaResponse>>())!;
        Assert.All(soEquipe, r => Assert.Equal("Equipe", r.Entidade));
    }

    [Fact]
    public async Task EquipeEAuditoriaNaoAtravessamEmpresas()
    {
        var gestorA = await RegistrarGestorAsync($"eqiA.{Guid.NewGuid():N}@exemplo.com");
        var gestorB = await RegistrarGestorAsync($"eqiB.{Guid.NewGuid():N}@exemplo.com");
        var (idDeA, _) = await CriarMembroAsync(
            gestorA, Papeis.Tecnico, $"tecA.{Guid.NewGuid():N}@exemplo.com");

        // B não vê o membro de A nem consegue mexer nele.
        var equipeDeB = (await (await EnviarAsync(HttpMethod.Get, "/api/equipe", gestorB))
            .Content.ReadFromJsonAsync<List<EquipeMembroResponse>>())!;
        Assert.DoesNotContain(equipeDeB, m => m.Id == idDeA);

        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Delete, $"/api/equipe/{idDeA}", gestorB)).StatusCode);

        // E a trilha de A não aparece para B.
        var auditoriaDeB = (await (await EnviarAsync(HttpMethod.Get, "/api/auditoria", gestorB))
            .Content.ReadFromJsonAsync<List<RegistroAuditoriaResponse>>())!;
        Assert.Empty(auditoriaDeB);
    }
}
