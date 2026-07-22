using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
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

    [Authorize(Policy = Politicas.Atendimento)]
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

    [Authorize(Policy = Politicas.Atendimento)]
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

    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(int id) =>
        TraduzirResultado(await service.CheckInAsync(
            id, Guid.TryParse(User.FindFirstValue("sub"), out var usuarioId) ? usuarioId : null));

    [HttpPost("{id:int}/cancelar")]

    [Authorize(Policy = Politicas.Atendimento)]
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

    /// <summary>Cliente não apareceu — estado terminal distinto de cancelamento.</summary>
    [HttpPost("{id:int}/nao-compareceu")]
    [Authorize(Policy = Politicas.Atendimento)]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NaoCompareceu(int id) =>
        TraduzirResultado(await service.RegistrarNaoComparecimentoAsync(id));

    /// <summary>Histórico de comparecimento do cliente (rota client-centric).</summary>
    [HttpGet("/api/clientes/{clienteId:int}/comparecimento")]
    [ProducesResponseType<ComparecimentoResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Comparecimento(int clienteId) =>
        Ok(await service.ComparecimentoDoClienteAsync(clienteId));

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
