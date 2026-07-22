using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Acompanhamento público da OS (módulo 1, Fase 1): o cliente abre o link
/// com slug + código opaco, sem login. O slug resolve o tenant (mesmo padrão
/// do agendamento público) e o código não enumerável localiza a OS já sob
/// GQF+RLS. A resposta expõe só status — nada de dados pessoais.
/// </summary>
[ApiController]
[Route("api/publico/{slug}/acompanhar")]
[AllowAnonymous]
[EnableRateLimiting("publico")]
[Produces("application/json")]
public class AcompanhamentoController(
    TechProDbContext db,
    TenantAmbiente tenantAmbiente,
    FinanceiroService financeiro,
    Reputacao.AvaliacaoService avaliacoes,
    IValidator<RespostaOrcamentoRequest> validadorResposta,
    IValidator<Reputacao.Dtos.AvaliacaoRequest> validadorAvaliacao) : ControllerBase
{
    [HttpGet("{codigo}")]
    [ProducesResponseType<AcompanhamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(string slug, string codigo)
    {
        var ordem = await ResolverOrdemAsync(slug, codigo);
        if (ordem is null)
        {
            return NotFound();
        }

        // Linha do tempo client-safe: cada etapa percorrida com a 1ª vez que foi
        // alcançada. Sem usuário/motivo (podem ter nota interna). Ordenação em
        // memória — Sqlite (testes) não ordena DateTimeOffset no servidor.
        var linhaDoTempo = (await db.HistoricosEtapaOrdemServico
                .Where(h => h.OrdemServicoId == ordem.Id && h.DeletedAt == null)
                .Select(h => new { h.ParaEtapa, h.CriadoEm })
                .ToListAsync())
            .GroupBy(h => h.ParaEtapa)
            .Select(g => new EtapaAlcancadaResponse(g.Key, g.Min(h => h.CriadoEm)))
            .OrderBy(e => e.AlcancadaEm)
            .ToList();

        var jaAvaliada = await avaliacoes.JaAvaliadaAsync(ordem.Id);
        return Ok(new AcompanhamentoResponse(
            _empresa!.Nome,
            ordem.Numero,
            ordem.Servico!.Nome,
            ordem.Etapa,
            ordem.PrazoEstimado,
            ordem.UpdatedAt,
            await financeiro.ObterOrcamentoPublicoAsync(ordem.Id),
            new LojaContatoResponse(
                _empresa.Telefone, _empresa.Email, _empresa.Endereco, _empresa.Politicas),
            linhaDoTempo,
            // Só avalia depois de entregue e uma vez.
            PodeAvaliar: ordem.Etapa == EtapaOrdemServico.Entregue && !jaAvaliada,
            JaAvaliada: jaAvaliada));
    }

    /// <summary>Avaliação do cliente pelo link público (após a entrega).</summary>
    [HttpPost("{codigo}/avaliacao")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Avaliar(
        string slug, string codigo, Reputacao.Dtos.AvaliacaoRequest request)
    {
        var validacao = await validadorAvaliacao.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var ordem = await ResolverOrdemAsync(slug, codigo);
        if (ordem is null)
        {
            return NotFound();
        }

        var resultado = await avaliacoes.RegistrarAsync(ordem, request);
        return resultado.Erro is not null
            ? Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest)
            : Created($"/api/publico/{slug}/acompanhar/{codigo}", null);
    }

    /// <summary>Aprovação binária pelo cliente final (módulo 1, Fase 1) — com trilha.</summary>
    [HttpPost("{codigo}/orcamento/aprovar")]
    [ProducesResponseType<OrcamentoPublicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Aprovar(string slug, string codigo, RespostaOrcamentoRequest request) =>
        ResponderAsync(slug, codigo, request, aprovado: true);

    [HttpPost("{codigo}/orcamento/recusar")]
    [ProducesResponseType<OrcamentoPublicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Recusar(string slug, string codigo, RespostaOrcamentoRequest request) =>
        ResponderAsync(slug, codigo, request, aprovado: false);

    private async Task<IActionResult> ResponderAsync(
        string slug, string codigo, RespostaOrcamentoRequest request, bool aprovado)
    {
        var validacao = await validadorResposta.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var ordem = await ResolverOrdemAsync(slug, codigo);
        if (ordem is null)
        {
            return NotFound();
        }

        var resultado = await financeiro.ResponderAsync(
            ordem.Id, aprovado, request.Motivo, usuarioId: null, CanalEventoOrcamento.Portal);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(await financeiro.ObterOrcamentoPublicoAsync(ordem.Id));
    }

    private Empresa? _empresa;

    /// <summary>
    /// Slug resolve o tenant (ignora o GQF da Empresa de propósito — ainda não
    /// há tenant) e o código opaco localiza a OS já sob GQF+RLS.
    /// </summary>
    private async Task<OrdemServico?> ResolverOrdemAsync(string slug, string codigo)
    {
        var empresa = await db.Empresas
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => e.Slug == slug);
        if (empresa is null)
        {
            return null;
        }

        tenantAmbiente.TenantIdFixado = empresa.Id;
        _empresa = empresa;

        return await db.OrdensServico
            .Include(o => o.Servico)
            .FirstOrDefaultAsync(o => o.CodigoAcompanhamento == codigo && o.DeletedAt == null);
    }
}
