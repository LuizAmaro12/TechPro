using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Agendamentos;

[ApiController]
[Route("api/agendamentos")]
[Authorize]
[Produces("application/json")]
public class AgendamentosController(
    AgendamentoService service,
    IValidator<AgendamentoRequest> validador,
    IValidator<CancelamentoRequest> validadorCancelamento) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<AgendamentoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        [FromQuery] StatusAgendamento? status) =>
        Ok(await service.ListarAsync(inicio, fim, status));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } agendamento ? Ok(agendamento) : NotFound();

    [HttpPost]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(AgendamentoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.CriarManualAsync(request);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/agendamentos/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, AgendamentoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return TraduzirResultado(await service.AtualizarAsync(id, request));
    }

    [HttpPost("{id:int}/checkin")]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(int id) =>
        TraduzirResultado(await service.CheckInAsync(id));

    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(int id, CancelamentoRequest request)
    {
        var validacao = await validadorCancelamento.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return TraduzirResultado(await service.CancelarAsync(id, request));
    }

    private IActionResult TraduzirResultado(CatalogoResultado<AgendamentoResponse>? resultado)
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
