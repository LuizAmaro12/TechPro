using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Financeiro;

/// <summary>Orçamento e pagamentos da OS (lado da loja).</summary>
[ApiController]
[Route("api/ordens-servico/{ordemId:guid}")]
[Authorize]
[Produces("application/json")]
public class FinanceiroController(
    FinanceiroService service,
    IValidator<OrcamentoRequest> validadorOrcamento,
    IValidator<RespostaOrcamentoRequest> validadorResposta,
    IValidator<PagamentoRequest> validadorPagamento) : ControllerBase
{
    private Guid? UsuarioId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpPut("orcamento")]

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SalvarOrcamento(Guid ordemId, OrcamentoRequest request)
    {
        var validacao = await validadorOrcamento.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return Traduzir(await service.SalvarRascunhoAsync(ordemId, request));
    }

    [HttpPost("orcamento/enviar")]

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnviarOrcamento(Guid ordemId) =>
        Traduzir(await service.EnviarAsync(ordemId, UsuarioId));

    [HttpPost("orcamento/aprovar")]

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AprovarOrcamento(Guid ordemId, RespostaOrcamentoRequest request)
    {
        var validacao = await validadorResposta.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return Traduzir(await service.ResponderAsync(
            ordemId, aprovado: true, request.Motivo, UsuarioId, CanalEventoOrcamento.Loja));
    }

    [HttpPost("orcamento/recusar")]

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<OrcamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecusarOrcamento(Guid ordemId, RespostaOrcamentoRequest request)
    {
        var validacao = await validadorResposta.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return Traduzir(await service.ResponderAsync(
            ordemId, aprovado: false, request.Motivo, UsuarioId, CanalEventoOrcamento.Loja));
    }

    [HttpPost("pagamentos")]

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<ResumoPagamentosResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegistrarPagamento(Guid ordemId, PagamentoRequest request)
    {
        var validacao = await validadorPagamento.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.RegistrarPagamentoAsync(ordemId, request, UsuarioId);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/ordens-servico/{ordemId}/pagamentos", resultado.Valor);
    }

    [HttpDelete("pagamentos/{pagamentoId:int}")]

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<ResumoPagamentosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverPagamento(Guid ordemId, int pagamentoId)
    {
        var resultado = await service.RemoverPagamentoAsync(ordemId, pagamentoId);
        if (resultado is null)
        {
            return NotFound();
        }

        return Ok(resultado.Valor);
    }

    private IActionResult Traduzir(CatalogoResultado<OrcamentoResponse>? resultado)
    {
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(resultado.Valor);
    }
}
