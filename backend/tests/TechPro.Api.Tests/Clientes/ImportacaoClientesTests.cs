using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Clientes;

/// <summary>
/// Etapa "importação de clientes por CSV" (Fase 2): a porta de entrada do
/// produto. Só adiciona; dedup por telefone; relatório por linha.
/// </summary>
public class ImportacaoClientesTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
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

    private async Task<ImportacaoClientesResponse> ImportarAsync(string token, string csv) =>
        (await (await EnviarAsync(HttpMethod.Post, "/api/clientes/importar", token,
            new { conteudoCsv = csv })).Content.ReadFromJsonAsync<ImportacaoClientesResponse>())!;

    private async Task<PaginaResponseDeCliente> ListarAsync(string token) =>
        (await (await EnviarAsync(HttpMethod.Get, "/api/clientes?tamanhoPagina=100", token))
            .Content.ReadFromJsonAsync<PaginaResponseDeCliente>())!;

    [Fact]
    public async Task ImportaLinhasValidasEPreencheOsCampos()
    {
        var token = await RegistrarEmpresaAsync($"imp.{Guid.NewGuid():N}@exemplo.com");
        var csv = string.Join("\n",
            "nome,telefone,email,cpf,endereco,observacoes",
            "Maria Souza,(11) 99999-0001,maria@ex.com,,Rua A 10,cliente antiga",
            "João Lima,(11) 99999-0002,,,,");

        var r = await ImportarAsync(token, csv);
        Assert.Equal(2, r.Total);
        Assert.Equal(2, r.Importados);
        Assert.Equal(0, r.Duplicados);
        Assert.Empty(r.Erros);

        var lista = await ListarAsync(token);
        var maria = lista.Itens.Single(c => c.Nome == "Maria Souza");
        Assert.Equal("maria@ex.com", maria.Email);
        Assert.Equal("cliente antiga", maria.Observacoes);
        // Importar não concede consentimento LGPD.
        Assert.False(maria.ConsentiuComunicacoes);
    }

    [Fact]
    public async Task DeduplicaContraOBancoEDentroDoArquivo()
    {
        var token = await RegistrarEmpresaAsync($"dedup.{Guid.NewGuid():N}@exemplo.com");
        // Já existe no banco (mesmo telefone, formatação diferente).
        await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Existente",
            telefone = "11999990001",
            email = "",
            cpf = "",
            endereco = "",
            observacoes = "",
            vip = false,
            clientePrincipalId = (int?)null,
            consentiuComunicacoes = false,
        });

        var csv = string.Join("\n",
            "nome,telefone",
            "Repetida do banco,(11) 99999-0001",   // dup contra o banco
            "Nova,(11) 98888-0002",
            "Nova de novo,11 98888 0002");          // dup dentro do arquivo

        var r = await ImportarAsync(token, csv);
        Assert.Equal(3, r.Total);
        Assert.Equal(1, r.Importados);   // só "Nova"
        Assert.Equal(2, r.Duplicados);
        Assert.Empty(r.Erros);
    }

    [Fact]
    public async Task LinhaSemNomeOuTelefoneVaiParaOsErrosSemBloquearOResto()
    {
        var token = await RegistrarEmpresaAsync($"erro.{Guid.NewGuid():N}@exemplo.com");
        var csv = string.Join("\n",
            "nome,telefone",
            "Sem telefone,",
            ",(11) 90000-0000",
            "Telefone curto,123",
            "Boa,(11) 91111-2222");

        var r = await ImportarAsync(token, csv);
        Assert.Equal(4, r.Total);
        Assert.Equal(1, r.Importados);
        Assert.Equal(3, r.Erros.Count);
        // Números de linha são os que o usuário vê (base-1, com cabeçalho).
        Assert.Contains(r.Erros, e => e.Linha == 2);
        Assert.Contains(r.Erros, e => e.Linha == 4 && e.Motivo.Contains("inválido"));
    }

    [Fact]
    public async Task AceitaDelimitadorPontoVirgulaCabecalhoSemAcentoEAspas()
    {
        var token = await RegistrarEmpresaAsync($"delim.{Guid.NewGuid():N}@exemplo.com");
        // Excel BR: separador ';', acento no cabeçalho, campo com vírgula entre aspas.
        var csv = string.Join("\n",
            "Nome;Telefone;Observações",
            "Ana Paula;(11) 97777-0001;\"Cliente VIP, sempre paga à vista\"");

        var r = await ImportarAsync(token, csv);
        Assert.Equal(1, r.Importados);
        var lista = await ListarAsync(token);
        Assert.Equal("Cliente VIP, sempre paga à vista",
            lista.Itens.Single().Observacoes);
    }

    [Fact]
    public async Task CabecalhoSemNomeOuTelefoneFalhaInteira()
    {
        var token = await RegistrarEmpresaAsync($"semcab.{Guid.NewGuid():N}@exemplo.com");
        var resposta = await EnviarAsync(HttpMethod.Post, "/api/clientes/importar", token,
            new { conteudoCsv = "apelido,cidade\nZé,SP" });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task ImportacaoNaoAtravessaEmpresas()
    {
        var tokenA = await RegistrarEmpresaAsync($"impA.{Guid.NewGuid():N}@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync($"impB.{Guid.NewGuid():N}@exemplo.com");

        await ImportarAsync(tokenA, "nome,telefone\nDa Loja A,(11) 91234-0001");

        // Mesmo telefone: para B não é duplicado (o de A não existe no tenant de B).
        var r = await ImportarAsync(tokenB, "nome,telefone\nDa Loja B,(11) 91234-0001");
        Assert.Equal(1, r.Importados);
        Assert.Equal(0, r.Duplicados);

        Assert.DoesNotContain((await ListarAsync(tokenB)).Itens, c => c.Nome == "Da Loja A");
    }

    private record PaginaResponseDeCliente(List<ClienteResponse> Itens, int Total);
}
