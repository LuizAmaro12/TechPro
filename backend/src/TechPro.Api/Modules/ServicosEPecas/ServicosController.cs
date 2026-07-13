using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.ServicosEPecas;

[ApiController]
[Route("api/servicos")]
[Authorize]
[Produces("application/json")]
public class ServicosController(ServicoService service, IValidator<ServicoRequest> validador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaResponse<ServicoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? categoria,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);
        return Ok(await service.ListarAsync(busca, categoria, incluirInativos, pagina, tamanhoPagina));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } servico ? Ok(servico) : NotFound();

    [HttpPost]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(ServicoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.CriarAsync(request);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/servicos/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, ServicoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.AtualizarAsync(id, request);
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

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int id) =>
        await service.DesativarAsync(id) ? NoContent() : NotFound();
}
