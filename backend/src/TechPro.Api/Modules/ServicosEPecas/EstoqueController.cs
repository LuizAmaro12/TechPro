using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.ServicosEPecas.Dtos;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Visões de estoque que atravessam peças — hoje a lista de compra. Fica fora
/// de <c>PecasController</c> porque não é operação sobre uma peça específica.
/// </summary>
[ApiController]
[Route("api/estoque")]
[Authorize(Policy = Politicas.Bancada)]
[Produces("application/json")]
public class EstoqueController(EstoqueService estoque) : ControllerBase
{
    /// <summary>Peças no/abaixo do mínimo agrupadas por fornecedor — a loja
    /// compra por fornecedor, não peça a peça.</summary>
    [HttpGet("lista-compra")]
    [ProducesResponseType<ListaCompraResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListaDeCompra() => Ok(await estoque.ListaDeCompraAsync());
}
