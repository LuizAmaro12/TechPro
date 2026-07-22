using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.ServicosEPecas;

[ApiController]
[Route("api/fornecedores")]
[Authorize(Policy = Politicas.Bancada)]
[Produces("application/json")]
public class FornecedoresController(
    FornecedorService service,
    IValidator<FornecedorRequest> validador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<FornecedorResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpPost]
    [ProducesResponseType<FornecedorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(FornecedorRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var fornecedor = await service.CriarAsync(request);
        return Created($"/api/fornecedores/{fornecedor.Id}", fornecedor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<FornecedorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, FornecedorRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var fornecedor = await service.AtualizarAsync(id, request);
        return fornecedor is null ? NotFound() : Ok(fornecedor);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id) => await service.RemoverAsync(id) switch
    {
        FornecedorService.Remocao.Removido => NoContent(),
        FornecedorService.Remocao.EmUso => Problem(
            title: "Este fornecedor tem peças vinculadas e não pode ser removido.",
            statusCode: StatusCodes.Status409Conflict),
        _ => NotFound(),
    };
}
