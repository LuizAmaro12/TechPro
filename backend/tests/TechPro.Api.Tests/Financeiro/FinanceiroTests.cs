using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Financeiro;

/// <summary>
/// Orçamento (trilha append-only, aprovação pela loja e pelo portal) e
/// pagamentos parciais com status derivado.
/// </summary>
public class FinanceiroTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    private async Task<(string Token, string Slug)> RegistrarEmpresaAsync(string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = $"Loja de {email}",
            nome = "Dono",
            email,
            senha = "senha123",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var token = (await resposta.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        var configuracao = await EnviarAsync(HttpMethod.Get, "/api/agenda/configuracoes", token);
        var slug = (await configuracao.Content
            .ReadFromJsonAsync<TechPro.Api.Modules.Agendamentos.Dtos.ConfiguracaoAgendaResponse>())!.Slug;
        return (token, slug);
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

    /// <summary>OS com uma peça usada de venda 40 (2 × 20).</summary>
    private async Task<OrdemServicoResponse> CriarOsComPecaAsync(string token)
    {
        var cliente = await EnviarAsync(HttpMethod.Post, "/api/clientes", token, new
        {
            nome = "Maria Souza", telefone = "(11) 99999-0000", email = "", cpf = "",
            endereco = "", observacoes = "", vip = false,
            clientePrincipalId = (int?)null, consentiuComunicacoes = false,
        });
        var clienteId = (await cliente.Content.ReadFromJsonAsync<ClienteResponse>())!.Id;

        var peca = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome = "Conector", descricao = (string?)null, custoUnitario = 8m,
            precoVenda = 20m, quantidadeEmEstoque = 10, estoqueMinimo = 1,
            fornecedorId = (int?)null, ativo = true,
        });
        var pecaId = (await peca.Content.ReadFromJsonAsync<PecaResponse>())!.Id;

        var servico = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, new
        {
            nome = "Reparo de conector", categoria = "Reparo", precoBase = 150m,
            duracaoEstimadaMinutos = 30, prazoMedioDias = (int?)null,
            exigeDiagnostico = false, agendavelOnline = false, capacidadeSimultanea = 1,
            ativo = true, checklist = Array.Empty<string>(), pecas = Array.Empty<object>(),
        });
        var servicoId = (await servico.Content.ReadFromJsonAsync<ServicoResponse>())!.Id;

        var os = await EnviarAsync(HttpMethod.Post, "/api/ordens-servico", token, new
        {
            clienteId, servicoId, aparelhoId = (int?)null,
            aparelhoMarca = (string?)null, aparelhoModelo = (string?)null,
            descricaoProblema = (string?)null, prioridade = "Normal",
            prazoEstimado = (string?)null, responsavelTecnicoId = (Guid?)null,
            observacoes = (string?)null,
        });
        var ordem = (await os.Content.ReadFromJsonAsync<OrdemServicoResponse>())!;

        var usar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{ordem.Id}/pecas",
            token, new { pecaId, quantidade = 2 });
        Assert.Equal(HttpStatusCode.Created, usar.StatusCode);
        return ordem;
    }

    private async Task<OrcamentoResponse> SalvarEEnviarOrcamentoAsync(
        string token, Guid ordemId, decimal maoDeObra = 100m, decimal desconto = 10m)
    {
        var salvar = await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{ordemId}/orcamento",
            token, new { valorMaoDeObra = maoDeObra, desconto });
        Assert.Equal(HttpStatusCode.OK, salvar.StatusCode);

        var enviar = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{ordemId}/orcamento/enviar", token);
        Assert.Equal(HttpStatusCode.OK, enviar.StatusCode);
        return (await enviar.Content.ReadFromJsonAsync<OrcamentoResponse>())!;
    }

    [Fact]
    public async Task EnvioCongelaPecasMoveEtapaERegistraTrilha()
    {
        var (token, _) = await RegistrarEmpresaAsync("orcamento.envio@exemplo.com");
        var os = await CriarOsComPecaAsync(token);

        // Rascunho: total ao vivo = 100 (mão de obra) + 40 (peças) − 10 = 130.
        var salvar = await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}/orcamento",
            token, new { valorMaoDeObra = 100m, desconto = 10m });
        var rascunho = await salvar.Content.ReadFromJsonAsync<OrcamentoResponse>();
        Assert.Equal(StatusOrcamento.Rascunho, rascunho!.Status);
        Assert.Equal(130m, rascunho.Total);

        var enviado = await SalvarEEnviarOrcamentoAsync(token, os.Id);
        Assert.Equal(StatusOrcamento.Enviado, enviado.Status);
        Assert.Equal(40m, enviado.ValorPecas);
        var evento = Assert.Single(enviado.Eventos);
        Assert.Equal(TipoEventoOrcamento.Enviado, evento.Tipo);
        Assert.Equal(CanalEventoOrcamento.Loja, evento.Canal);
        Assert.Equal("Dono", evento.UsuarioNome);
        Assert.Equal(130m, evento.ValorTotal);

        // Só o envio move etapa: OS agora em Aguardando aprovação, com trilha.
        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(EtapaOrdemServico.AguardandoAprovacao, corpo!.Ordem.Etapa);
        Assert.Contains(corpo.Historico, h => h.Motivo == "Orçamento enviado");

        // Peça registrada depois do envio não muda o orçamento congelado.
        var pecaNova = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
        {
            nome = "Película", descricao = (string?)null, custoUnitario = 2m,
            precoVenda = 10m, quantidadeEmEstoque = 5, estoqueMinimo = 0,
            fornecedorId = (int?)null, ativo = true,
        });
        var pecaNovaId = (await pecaNova.Content.ReadFromJsonAsync<PecaResponse>())!.Id;
        await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pecas",
            token, new { pecaId = pecaNovaId, quantidade = 1 });

        detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(130m, corpo!.Orcamento!.Total);
    }

    [Fact]
    public async Task AprovacaoPelaLojaEEdicaoDepoisVoltaARascunho()
    {
        var (token, _) = await RegistrarEmpresaAsync("orcamento.loja@exemplo.com");
        var os = await CriarOsComPecaAsync(token);
        await SalvarEEnviarOrcamentoAsync(token, os.Id);

        var aprovar = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/aprovar",
            token, new { motivo = "Aprovado pelo WhatsApp" });
        Assert.Equal(HttpStatusCode.OK, aprovar.StatusCode);
        var aprovado = await aprovar.Content.ReadFromJsonAsync<OrcamentoResponse>();
        Assert.Equal(StatusOrcamento.Aprovado, aprovado!.Status);
        Assert.Equal(2, aprovado.Eventos.Count);

        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(StatusAprovacaoOrdemServico.Aprovado, corpo!.Ordem.StatusAprovacao);

        // Aprovar de novo → 400 (já respondido).
        var repetido = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/aprovar",
            token, new { motivo = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);

        // Editar depois de aprovado volta a Rascunho (trilha preservada).
        var editar = await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}/orcamento",
            token, new { valorMaoDeObra = 200m, desconto = 0m });
        var revisado = await editar.Content.ReadFromJsonAsync<OrcamentoResponse>();
        Assert.Equal(StatusOrcamento.Rascunho, revisado!.Status);
        Assert.Equal(2, revisado.Eventos.Count);

        detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(StatusAprovacaoOrdemServico.Pendente, corpo!.Ordem.StatusAprovacao);
    }

    [Fact]
    public async Task AprovacaoPeloPortalComTrilhaDeCanal()
    {
        var (token, slug) = await RegistrarEmpresaAsync("orcamento.portal@exemplo.com");
        var os = await CriarOsComPecaAsync(token);

        // Antes do envio o portal não mostra orçamento (rascunho é interno).
        var antes = await _cliente.GetAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}");
        var statusAntes = await antes.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.Null(statusAntes!.Orcamento);

        await SalvarEEnviarOrcamentoAsync(token, os.Id);

        var depois = await _cliente.GetAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}");
        var statusDepois = await depois.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        Assert.Equal(130m, statusDepois!.Orcamento!.Total);
        Assert.Equal(StatusOrcamento.Enviado, statusDepois.Orcamento.Status);

        // Cliente aprova sem login.
        var aprovar = await _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}/orcamento/aprovar",
            new { motivo = (string?)null });
        Assert.Equal(HttpStatusCode.OK, aprovar.StatusCode);
        var aprovado = await aprovar.Content.ReadFromJsonAsync<OrcamentoPublicoResponse>();
        Assert.Equal(StatusOrcamento.Aprovado, aprovado!.Status);

        // Trilha registra canal Portal, sem usuário.
        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        var evento = corpo!.Orcamento!.Eventos.Last();
        Assert.Equal(TipoEventoOrcamento.Aprovado, evento.Tipo);
        Assert.Equal(CanalEventoOrcamento.Portal, evento.Canal);
        Assert.Null(evento.UsuarioNome);

        // Responder de novo pelo portal → 400.
        var repetido = await _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}/orcamento/recusar",
            new { motivo = "mudei de ideia" });
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);
    }

    [Fact]
    public async Task RecusaPeloPortalGuardaMotivo()
    {
        var (token, slug) = await RegistrarEmpresaAsync("orcamento.recusa@exemplo.com");
        var os = await CriarOsComPecaAsync(token);
        await SalvarEEnviarOrcamentoAsync(token, os.Id);

        var recusar = await _cliente.PostAsJsonAsync(
            $"/api/publico/{slug}/acompanhar/{os.CodigoAcompanhamento}/orcamento/recusar",
            new { motivo = "Ficou caro" });
        Assert.Equal(HttpStatusCode.OK, recusar.StatusCode);

        var detalhe = await EnviarAsync(HttpMethod.Get, $"/api/ordens-servico/{os.Id}", token);
        var corpo = await detalhe.Content.ReadFromJsonAsync<OrdemServicoDetalheResponse>();
        Assert.Equal(StatusOrcamento.Recusado, corpo!.Orcamento!.Status);
        Assert.Equal("Ficou caro", corpo.Orcamento.MotivoRecusa);
        Assert.Equal(StatusAprovacaoOrdemServico.Recusado, corpo.Ordem.StatusAprovacao);
    }

    [Fact]
    public async Task PagamentosParciaisDerivamStatusEPodemSerRemovidos()
    {
        var (token, _) = await RegistrarEmpresaAsync("pagamento.parcial@exemplo.com");
        var os = await CriarOsComPecaAsync(token);
        await SalvarEEnviarOrcamentoAsync(token, os.Id); // total 130

        var primeiro = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pagamentos",
            token, new { valor = 50m, forma = "Pix", observacao = "Entrada" });
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        var resumo = await primeiro.Content.ReadFromJsonAsync<ResumoPagamentosResponse>();
        Assert.Equal(StatusPagamentoOrdemServico.Parcial, resumo!.Status);
        Assert.Equal(80m, resumo.Saldo);

        var segundo = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pagamentos",
            token, new { valor = 80m, forma = "Dinheiro", observacao = (string?)null });
        resumo = await segundo.Content.ReadFromJsonAsync<ResumoPagamentosResponse>();
        Assert.Equal(StatusPagamentoOrdemServico.Pago, resumo!.Status);
        Assert.Equal(0m, resumo.Saldo);
        Assert.Equal("Dono", resumo.Pagamentos[0].RegistradoPorNome);

        // Remover um pagamento recalcula para Parcial.
        var remover = await EnviarAsync(HttpMethod.Delete,
            $"/api/ordens-servico/{os.Id}/pagamentos/{resumo.Pagamentos[1].Id}", token);
        Assert.Equal(HttpStatusCode.OK, remover.StatusCode);
        resumo = await remover.Content.ReadFromJsonAsync<ResumoPagamentosResponse>();
        Assert.Equal(StatusPagamentoOrdemServico.Parcial, resumo!.Status);
        Assert.Equal(50m, resumo.TotalPago);
    }

    [Fact]
    public async Task PagamentoSemOrcamentoNuncaQuitaAutomaticamente()
    {
        var (token, _) = await RegistrarEmpresaAsync("pagamento.semorcamento@exemplo.com");
        var os = await CriarOsComPecaAsync(token);

        var pagar = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pagamentos",
            token, new { valor = 500m, forma = "CartaoCredito", observacao = (string?)null });
        var resumo = await pagar.Content.ReadFromJsonAsync<ResumoPagamentosResponse>();
        // Sem total para quitar, o máximo é Parcial.
        Assert.Equal(StatusPagamentoOrdemServico.Parcial, resumo!.Status);
        Assert.Null(resumo.TotalOrcamento);
    }

    [Fact]
    public async Task OrcamentoNaoVazaEntreEmpresas()
    {
        var (tokenA, _) = await RegistrarEmpresaAsync("financeiro.iso.a@exemplo.com");
        var (tokenB, _) = await RegistrarEmpresaAsync("financeiro.iso.b@exemplo.com");
        var os = await CriarOsComPecaAsync(tokenA);
        await SalvarEEnviarOrcamentoAsync(tokenA, os.Id);

        var salvarB = await EnviarAsync(HttpMethod.Put, $"/api/ordens-servico/{os.Id}/orcamento",
            tokenB, new { valorMaoDeObra = 1m, desconto = 0m });
        Assert.Equal(HttpStatusCode.NotFound, salvarB.StatusCode);

        var aprovarB = await EnviarAsync(
            HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/aprovar",
            tokenB, new { motivo = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, aprovarB.StatusCode);

        var pagarB = await EnviarAsync(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/pagamentos",
            tokenB, new { valor = 10m, forma = "Pix", observacao = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, pagarB.StatusCode);
    }
}
