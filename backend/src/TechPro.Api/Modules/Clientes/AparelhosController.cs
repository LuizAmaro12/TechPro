using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.Clientes;

[ApiController]
[Route("api/clientes/{clienteId:int}/aparelhos")]
[Authorize]
[Produces("application/json")]
public class AparelhosController(AparelhoService service, IValidator<AparelhoRequest> validador) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AparelhoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Criar(int clienteId, AparelhoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var aparelho = await service.CriarAsync(clienteId, request);
        return aparelho is null
            ? NotFound()
            : Created($"/api/clientes/{clienteId}/aparelhos/{aparelho.Id}", aparelho);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AparelhoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int clienteId, int id, AparelhoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var aparelho = await service.AtualizarAsync(clienteId, id, request);
        return aparelho is null ? NotFound() : Ok(aparelho);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int clienteId, int id) =>
        await service.DesativarAsync(clienteId, id) ? NoContent() : NotFound();
}
