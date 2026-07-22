using Microsoft.AspNetCore.Authorization;
using TechPro.Api.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Clientes.Dtos;

namespace TechPro.Api.Modules.Clientes;

/// <summary>Direitos LGPD do cliente final: exportação e anonimização (módulo 14).</summary>
[ApiController]
[Route("api/clientes/{clienteId:int}")]
[Authorize(Policy = Politicas.Gestao)]
[Produces("application/json")]
public class LgpdController(LgpdService service, AuditoriaService auditoria) : ControllerBase
{
    [HttpGet("dados-pessoais")]
    [ProducesResponseType<DadosPessoaisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exportar(int clienteId)
    {
        if (await service.ExportarAsync(clienteId) is not { } dados)
        {
            return NotFound();
        }

        // Exportar dado pessoal é ato sensível: fica na trilha.
        await auditoria.RegistrarESalvarAsync(
            "Dados pessoais exportados", AreasAuditadas.Lgpd, clienteId.ToString());
        return Ok(dados);
    }

    [HttpPost("anonimizar")]
    [ProducesResponseType<ClienteDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Anonimizar(int clienteId)
    {
        if (await service.AnonimizarAsync(clienteId) is not { } cliente)
        {
            return NotFound();
        }

        await auditoria.RegistrarESalvarAsync(
            "Cliente anonimizado", AreasAuditadas.Lgpd, clienteId.ToString());
        return Ok(cliente);
    }
}
