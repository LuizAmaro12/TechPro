using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Agendamentos;

[ApiController]
[Route("api/fila-espera")]
[Authorize]
[Produces("application/json")]
public class FilaEsperaController(
    FilaEsperaService service,
    IValidator<FilaEsperaRequest> validador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<FilaEsperaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] StatusFilaEspera? status) =>
        Ok(await service.ListarAsync(status));

    [HttpPost]
    [ProducesResponseType<FilaEsperaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Entrar(FilaEsperaRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.EntrarManualAsync(request);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/fila-espera/{resultado.Valor!.Id}", resultado.Valor);
    }

    /// <summary>Abriu vaga: converte a entrada em agendamento na data/hora escolhida.</summary>
    [HttpPost("{id:int}/converter")]
    [ProducesResponseType<FilaEsperaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Converter(int id, ConverterFilaRequest request) =>
        Traduzir(await service.ConverterAsync(id, request));

    [HttpPost("{id:int}/descartar")]
    [ProducesResponseType<FilaEsperaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Descartar(int id, DescartarFilaRequest request) =>
        Traduzir(await service.DescartarAsync(id, request));

    private IActionResult Traduzir(CatalogoResultado<FilaEsperaResponse>? resultado)
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
