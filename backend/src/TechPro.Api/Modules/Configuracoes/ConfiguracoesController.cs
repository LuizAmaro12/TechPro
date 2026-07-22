using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Configuracoes;

[ApiController]
[Route("api/configuracoes")]
[Authorize(Policy = Politicas.Gestao)]
[Produces("application/json")]
public class ConfiguracoesController(
    ConfiguracoesService service,
    IValidator<LojaRequest> validadorLoja,
    IValidator<PreferenciasNotificacaoRequest> validadorPreferencias,
    AuditoriaService auditoria) : ControllerBase
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

        var salvo = await service.SalvarLojaAsync(request);
        await auditoria.RegistrarESalvarAsync("Dados da loja alterados", AreasAuditadas.Configuracoes);
        return Ok(salvo);
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

        var prefs = await service.SalvarPreferenciasAsync(request);
        await auditoria.RegistrarESalvarAsync("Preferências de notificação alteradas", AreasAuditadas.Configuracoes);
        return Ok(prefs);
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
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        await auditoria.RegistrarESalvarAsync(
            "Textos das mensagens alterados", AreasAuditadas.Configuracoes,
            detalhe: string.Join(", ", request.Itens.Select(i => i.TipoEvento)));
        return Ok(resultado.Valor);
    }
}
