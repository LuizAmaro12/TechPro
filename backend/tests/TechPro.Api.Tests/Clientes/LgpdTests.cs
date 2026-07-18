using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Clientes;

/// <summary>
/// Direitos LGPD (módulo 14): exportação (portabilidade) e exclusão por
/// anonimização, preservando o registro estrutural da OS.
/// </summary>
public class LgpdTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<ClienteResponse> CriarClienteAsync(string token) =>
        (await (await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria Souza",
            telefone = "(11) 99999-0000",
            email = "maria@exemplo.com",
            cpf = "529.982.247-25",
            endereco = "Rua das Flores, 123",
            observacoes = "Cliente antiga",
            vip = true,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = true,
        })).Content.ReadFromJsonAsync<ClienteResponse>())!;

    private async Task CriarAparelhoAsync(string token, int clienteId) =>
        Assert.Equal(HttpStatusCode.Created,
            (await EnviarAsync(HttpMethod.Post, $"/api/clientes/{clienteId}/aparelhos", token, new
            {
                marca = "Samsung",
                modelo = "Galaxy A54",
                imei = "356789104563218",
                senhaDesbloqueio = "1234",
                observacoes = "risco na tela",
            })).StatusCode);

    private async Task<OrdemServicoResponse> CriarOsAsync(string token, int clienteId)
    {
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
            aparelhoModelo = "Galaxy A54",
            descricaoProblema = "Tela trincada",
            prioridade = "Normal",
            prazoEstimado = (string?)null,
            responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, os.StatusCode);
        return (await os.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;
    }

    [Fact]
    public async Task ExportacaoTrazTodosOsDadosVinculados()
    {
        var token = await RegistrarEmpresaAsync("lgpd.export@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        await CriarAparelhoAsync(token, cliente.Id);
        var os = await CriarOsAsync(token, cliente.Id); // dispara mensagem (auditoria)

        var resposta = await EnviarAsync(
            HttpMethod.Get, $"/api/clientes/{cliente.Id}/dados-pessoais", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = await resposta.Content.ReadFromJsonAsync<DadosPessoaisResponse>();

        Assert.Equal("Maria Souza", dados!.Cliente.Nome);
        Assert.Equal("maria@exemplo.com", dados.Cliente.Email);
        Assert.Single(dados.Cliente.Aparelhos);
        Assert.Contains(dados.OrdensServico, o => o.Numero == os.Numero);
        // O cliente consentiu → a OS criada gerou ao menos uma notificação.
        Assert.NotEmpty(dados.Mensagens);
    }

    [Fact]
    public async Task AnonimizarLimpaDadosPessoaisEPreservaAOs()
    {
        var token = await RegistrarEmpresaAsync("lgpd.anon@exemplo.com");
        var cliente = await CriarClienteAsync(token);
        await CriarAparelhoAsync(token, cliente.Id);
        var os = await CriarOsAsync(token, cliente.Id);

        var resposta = await EnviarAsync(
            HttpMethod.Post, $"/api/clientes/{cliente.Id}/anonimizar", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var anonimo = await resposta.Content.ReadFromJsonAsync<ClienteDetalheResponse>();

        Assert.Equal($"Cliente anonimizado #{cliente.Id}", anonimo!.Nome);
        Assert.Null(anonimo.Email);
        Assert.Null(anonimo.Cpf);
        Assert.False(anonimo.Ativo);
        Assert.False(anonimo.ConsentiuComunicacoes);
        Assert.NotNull(anonimo.AnonimizadoEm);
        // Aparelho: IMEI e senha varridos.
        var aparelho = Assert.Single(anonimo.Aparelhos);
        Assert.Null(aparelho.Imei);
        Assert.Null(aparelho.SenhaDesbloqueio);

        // A OS (registro estrutural) continua existindo, com o número intacto.
        var detalheOs = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        Assert.Equal(HttpStatusCode.OK, detalheOs.StatusCode);
        var corpoOs = await detalheOs.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(os.Numero, corpoOs!.Ordem.Numero);

        // A exportação pós-anonimização já não traz PII, e as mensagens tiveram
        // o destino varrido.
        var exportado = await (await EnviarAsync(
                HttpMethod.Get, $"/api/clientes/{cliente.Id}/dados-pessoais", token))
            .Content.ReadFromJsonAsync<DadosPessoaisResponse>();
        Assert.DoesNotContain("maria@exemplo.com", System.Text.Json.JsonSerializer.Serialize(exportado));
        Assert.All(exportado!.Mensagens, m => Assert.Equal("anonimizado", m.Destino));
    }

    [Fact]
    public async Task AnonimizarEIdempotente()
    {
        var token = await RegistrarEmpresaAsync("lgpd.idem@exemplo.com");
        var cliente = await CriarClienteAsync(token);

        var primeira = await EnviarAsync(HttpMethod.Post, $"/api/clientes/{cliente.Id}/anonimizar", token);
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        var anonimo1 = await primeira.Content.ReadFromJsonAsync<ClienteDetalheResponse>();

        // Segunda chamada não quebra nem "re-anonimiza" com timestamp novo.
        var segunda = await EnviarAsync(HttpMethod.Post, $"/api/clientes/{cliente.Id}/anonimizar", token);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var anonimo2 = await segunda.Content.ReadFromJsonAsync<ClienteDetalheResponse>();
        // Mesmo instante (tolerância de ms — o conversor DateTimeOffset do Sqlite
        // trunca precisão no round-trip). Re-anonimizar geraria um timestamp ~80ms
        // depois (o tempo de uma chamada HTTP), que esta tolerância pegaria.
        Assert.True(
            (anonimo1!.AnonimizadoEm!.Value - anonimo2!.AnonimizadoEm!.Value).Duration()
                < TimeSpan.FromMilliseconds(5),
            "A anonimização não deve ser reaplicada (guarda de idempotência).");
    }

    [Fact]
    public async Task LgpdIsolaPorEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("lgpd.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("lgpd.iso.b@exemplo.com");
        var cliente = await CriarClienteAsync(tokenA);

        // B não exporta nem anonimiza cliente da A (GQF → 404).
        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Get, $"/api/clientes/{cliente.Id}/dados-pessoais", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await EnviarAsync(HttpMethod.Post, $"/api/clientes/{cliente.Id}/anonimizar", tokenB)).StatusCode);

        // E o cliente da A segue intacto.
        var aindaLa = await (await EnviarAsync(HttpMethod.Get, $"/api/clientes/{cliente.Id}", tokenA))
            .Content.ReadFromJsonAsync<ClienteDetalheResponse>();
        Assert.Equal("Maria Souza", aindaLa!.Nome);
        Assert.Null(aindaLa.AnonimizadoEm);
    }
}
