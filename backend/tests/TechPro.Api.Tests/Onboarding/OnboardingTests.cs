using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.Onboarding.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Onboarding;

public class OnboardingTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<OnboardingStatusResponse> StatusAsync(string token)
    {
        var resposta = await EnviarAsync(HttpMethod.Get, "/api/onboarding", token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OnboardingStatusResponse>())!;
    }

    [Fact]
    public async Task StatusInicialTemSoLojaConfiguradaEPendeConclusao()
    {
        var token = await RegistrarEmpresaAsync("onb.inicial@exemplo.com");
        var status = await StatusAsync(token);

        Assert.False(status.OnboardingConcluido);
        Assert.True(status.Passos.LojaConfigurada); // nome vem do cadastro
        Assert.False(status.Passos.HorariosConfigurados);
        Assert.False(status.Passos.TemServico);
        Assert.False(status.Passos.TemPeca);
        Assert.False(status.Passos.TemCliente);
        Assert.Equal(1, status.PassosConcluidos);
        Assert.Equal(5, status.TotalPassos);
        Assert.False(status.TemDadosExemplo);
    }

    [Fact]
    public async Task ConcluirMarcaOnboardingEEIdempotente()
    {
        var token = await RegistrarEmpresaAsync("onb.concluir@exemplo.com");

        Assert.Equal(HttpStatusCode.NoContent,
            (await EnviarAsync(HttpMethod.Post, "/api/onboarding/concluir", token)).StatusCode);
        Assert.True((await StatusAsync(token)).OnboardingConcluido);

        // Chamar de novo não quebra nem "reabre".
        Assert.Equal(HttpStatusCode.NoContent,
            (await EnviarAsync(HttpMethod.Post, "/api/onboarding/concluir", token)).StatusCode);
        Assert.True((await StatusAsync(token)).OnboardingConcluido);
    }

    [Fact]
    public async Task PassosRefletemOsDadosReais()
    {
        var token = await RegistrarEmpresaAsync("onb.passos@exemplo.com");

        await EnviarAsync(HttpMethod.Put, "/api/agenda/horarios", token, new
        {
            dias = Enumerable.Range(0, 7).Select(d => new
            {
                diaSemana = d,
                ativo = d != 0,
                abertura = "09:00:00",
                fechamento = "18:00:00",
                intervaloInicio = (string?)null,
                intervaloFim = (string?)null,
            }).ToList(),
        });
        await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
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
        await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome = "Tela",
            descricao = (string?)null,
            custoUnitario = 50m,
            precoVenda = 100m,
            quantidadeEmEstoque = 5,
            estoqueMinimo = 1,
            fornecedorId = (int?)null,
            ativo = true,
        });
        await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Cliente Real",
            telefone = "(11) 98888-7777",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });

        var status = await StatusAsync(token);
        Assert.True(status.Passos.HorariosConfigurados);
        Assert.True(status.Passos.TemServico);
        Assert.True(status.Passos.TemPeca);
        Assert.True(status.Passos.TemCliente);
        Assert.Equal(5, status.PassosConcluidos);
    }

    [Fact]
    public async Task DadosDeExemploCarregamRemovemESaoIdempotentes()
    {
        var token = await RegistrarEmpresaAsync("onb.exemplo@exemplo.com");

        Assert.Equal(HttpStatusCode.NoContent,
            (await EnviarAsync(HttpMethod.Post, "/api/onboarding/dados-exemplo", token)).StatusCode);

        var status = await StatusAsync(token);
        Assert.True(status.TemDadosExemplo);
        // Exemplo NÃO conta como passo real (serviço/cliente de verdade).
        Assert.False(status.Passos.TemServico);
        Assert.False(status.Passos.TemCliente);

        // A OS de exemplo aparece na listagem.
        var ordens = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", token);
        var lista = await ordens.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>();
        var os = Assert.Single(lista!);
        Assert.Contains("exemplo", os.ServicoNome, StringComparison.OrdinalIgnoreCase);

        // Idempotente: carregar de novo não duplica.
        await EnviarAsync(HttpMethod.Post, "/api/onboarding/dados-exemplo", token);
        ordens = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", token);
        Assert.Single((await ordens.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);

        // Remover limpa tudo.
        Assert.Equal(HttpStatusCode.NoContent,
            (await EnviarAsync(HttpMethod.Delete, "/api/onboarding/dados-exemplo", token)).StatusCode);
        Assert.False((await StatusAsync(token)).TemDadosExemplo);
        ordens = await EnviarAsync(HttpMethod.Get, "/api/ordens-servico", token);
        Assert.Empty((await ordens.Content.ReadFromJsonAsync<List<OrdemServicoResponse>>())!);

        var clientes = await EnviarAsync(HttpMethod.Get, "/api/clientes?incluirInativos=true", token);
        var pagina = await clientes.Content
            .ReadFromJsonAsync<TechPro.Api.Shared.Api.PaginaResponse<TechPro.Api.Modules.Clientes.Dtos.ClienteResponse>>();
        Assert.Equal(0, pagina!.Total);
    }

    [Fact]
    public async Task OnboardingIsolaPorEmpresa()
    {
        var tokenA = await RegistrarEmpresaAsync("onb.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("onb.iso.b@exemplo.com");

        await EnviarAsync(HttpMethod.Post, "/api/onboarding/concluir", tokenA);
        await EnviarAsync(HttpMethod.Post, "/api/onboarding/dados-exemplo", tokenA);

        // B segue intacto: não concluído e sem dados de exemplo.
        var statusB = await StatusAsync(tokenB);
        Assert.False(statusB.OnboardingConcluido);
        Assert.False(statusB.TemDadosExemplo);
    }
}
