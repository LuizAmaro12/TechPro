using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Clientes;

[ApiController]
[Route("api/clientes")]
[Authorize]
[Produces("application/json")]
public class ClientesController(
    ClienteService service,
    ImportacaoClientesService importacao,
    IValidator<ClienteRequest> validador) : ControllerBase
{
    /// <summary>Importa a carteira existente por CSV — só adiciona, com relatório
    /// por linha (duplicados e inválidos são pulados, não bloqueiam o resto).</summary>
    [HttpPost("importar")]
    [ProducesResponseType<ImportacaoClientesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Importar(ImportarClientesRequest request)
    {
        var resultado = await importacao.ImportarAsync(request.ConteudoCsv);
        return resultado.Erro is not null
            ? Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest)
            : Ok(resultado.Valor);
    }

    [HttpGet]
    [ProducesResponseType<PaginaResponse<ClienteResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] bool somenteVip = false,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);
        return Ok(await service.ListarAsync(busca, somenteVip, incluirInativos, pagina, tamanhoPagina));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ClienteDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } cliente ? Ok(cliente) : NotFound();

    [HttpPost]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(ClienteRequest request)
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

        return Created($"/api/clientes/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, ClienteRequest request)
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
