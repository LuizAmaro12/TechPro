using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Configuracoes;

[ApiController]
[Route("api/configuracoes")]
[Authorize]
[Produces("application/json")]
public class ConfiguracoesController(
    ConfiguracoesService service,
    IValidator<LojaRequest> validadorLoja,
    IValidator<PreferenciasNotificacaoRequest> validadorPreferencias) : ControllerBase
{
    [HttpGet("loja")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterLoja() => Ok(await service.ObterLojaAsync());

    [HttpPut("loja")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SalvarLoja(LojaRequest request)
    {
        var validacao = await validadorLoja.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return Ok(await service.SalvarLojaAsync(request));
    }

    [HttpGet("notificacoes")]
    [ProducesResponseType<PreferenciasNotificacaoResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPreferencias() =>
        Ok(await service.ObterPreferenciasAsync());

    [HttpPut("notificacoes")]
    [ProducesResponseType<PreferenciasNotificacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SalvarPreferencias(PreferenciasNotificacaoRequest request)
    {
        var validacao = await validadorPreferencias.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return Ok(await service.SalvarPreferenciasAsync(request));
    }

    // --- Templates de mensagem (Fase 2) --------------------------------------

    [HttpGet("templates")]
    [ProducesResponseType<TemplatesResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTemplates() => Ok(await service.ObterTemplatesAsync());

    /// <summary>Corpo vazio em um item volta aquele evento ao texto padrão.</summary>
    [HttpPut("templates")]
    [ProducesResponseType<TemplatesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SalvarTemplates(TemplatesRequest request)
    {
        var resultado = await service.SalvarTemplatesAsync(request);
        return resultado.Erro is not null
            ? Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest)
            : Ok(resultado.Valor);
    }
}
