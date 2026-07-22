using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Reputacao.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Reputacao;

[ApiController]
[Route("api/avaliacoes")]
[Authorize]
[Produces("application/json")]
public class AvaliacoesController(
    AvaliacaoService service,
    IValidator<ResolverAvaliacaoRequest> validadorResolucao) : ControllerBase
{
    private Guid? UsuarioId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet]
    [ProducesResponseType<List<AvaliacaoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool apenasPendentes = false) =>
        Ok(await service.ListarAsync(apenasPendentes));

    [HttpGet("resumo")]
    [ProducesResponseType<ResumoAvaliacoesResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Resumo() => Ok(await service.ResumoAsync());

    /// <summary>Fecha o loop de uma avaliação negativa com a nota de tratamento.</summary>
    [HttpPost("{id:int}/resolver")]
    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<AvaliacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolver(int id, ResolverAvaliacaoRequest request)
    {
        var validacao = await validadorResolucao.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.ResolverAsync(id, request, UsuarioId);
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
