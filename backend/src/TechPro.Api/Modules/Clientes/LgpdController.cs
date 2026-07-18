using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Clientes.Dtos;

namespace TechPro.Api.Modules.Clientes;

/// <summary>Direitos LGPD do cliente final: exportação e anonimização (módulo 14).</summary>
[ApiController]
[Route("api/clientes/{clienteId:int}")]
[Authorize]
[Produces("application/json")]
public class LgpdController(LgpdService service) : ControllerBase
{
    [HttpGet("dados-pessoais")]
    [ProducesResponseType<DadosPessoaisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exportar(int clienteId) =>
        await service.ExportarAsync(clienteId) is { } dados ? Ok(dados) : NotFound();

    [HttpPost("anonimizar")]
    [ProducesResponseType<ClienteDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Anonimizar(int clienteId) =>
        await service.AnonimizarAsync(clienteId) is { } cliente ? Ok(cliente) : NotFound();
}
