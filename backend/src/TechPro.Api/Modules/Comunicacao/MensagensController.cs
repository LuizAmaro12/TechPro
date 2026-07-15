using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Comunicacao.Dtos;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Auditoria das notificações de uma OS (registro mínimo da Fase 1; base do
/// inbox unificado por cliente da Fase 2).
/// </summary>
[ApiController]
[Route("api/ordens-servico/{ordemId:guid}/mensagens")]
[Authorize]
[Produces("application/json")]
public class MensagensController(TechProDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<MensagemEnviadaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(Guid ordemId)
    {
        var mensagens = await db.MensagensEnviadas
            .Where(m => m.OrdemServicoId == ordemId)
            .OrderByDescending(m => m.Id)
            .Select(m => new MensagemEnviadaResponse(
                m.Id, m.Canal, m.Destino, m.TipoEvento, m.Status, m.Erro, m.CriadoEm))
            .ToListAsync();
        return Ok(mensagens);
    }
}
