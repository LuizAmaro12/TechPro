using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>Configuração da agenda: horários, bloqueios, slug e disponibilidade.</summary>
[ApiController]
[Route("api/agenda")]
[Authorize]
[Produces("application/json")]
public class AgendaController(
    AgendaService service,
    DisponibilidadeService disponibilidade,
    IValidator<HorariosFuncionamentoRequest> validadorHorarios,
    IValidator<BloqueioRequest> validadorBloqueio,
    IValidator<ConfiguracaoAgendaRequest> validadorConfiguracao) : ControllerBase
{
    [HttpGet("horarios")]
    [ProducesResponseType<List<HorarioFuncionamentoDia>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarHorarios() =>
        Ok(await service.ListarHorariosAsync());

    [HttpPut("horarios")]

    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<List<HorarioFuncionamentoDia>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SalvarHorarios(HorariosFuncionamentoRequest request)
    {
        var validacao = await validadorHorarios.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return Ok(await service.SalvarHorariosAsync(request));
    }

    [HttpGet("bloqueios")]
    [ProducesResponseType<List<BloqueioResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarBloqueios(
        [FromQuery] DateOnly? deData, [FromQuery] DateOnly? ateData) =>
        Ok(await service.ListarBloqueiosAsync(deData, ateData));

    [HttpPost("bloqueios")]

    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<BloqueioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarBloqueio(BloqueioRequest request)
    {
        var validacao = await validadorBloqueio.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var bloqueio = await service.CriarBloqueioAsync(request);
        return Created($"/api/agenda/bloqueios/{bloqueio.Id}", bloqueio);
    }

    [HttpDelete("bloqueios/{id:int}")]

    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverBloqueio(int id) =>
        await service.RemoverBloqueioAsync(id) ? NoContent() : NotFound();

    [HttpGet("configuracoes")]
    [ProducesResponseType<ConfiguracaoAgendaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterConfiguracoes() =>
        Ok(await service.ObterConfiguracaoAsync());

    [HttpPut("configuracoes")]

    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<ConfiguracaoAgendaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AtualizarConfiguracoes(ConfiguracaoAgendaRequest request)
    {
        var validacao = await validadorConfiguracao.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.AtualizarSlugAsync(request);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(resultado.Valor);
    }

    [HttpGet("disponibilidade")]
    [ProducesResponseType<DisponibilidadeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disponibilidade(
        [FromQuery] int servicoId, [FromQuery] DateOnly data)
    {
        var resultado = await disponibilidade.CalcularAsync(servicoId, data);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(resultado.Valor);
    }
}
