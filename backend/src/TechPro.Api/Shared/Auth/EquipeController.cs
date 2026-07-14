using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Usuários da própria empresa (para o select de responsável técnico).
/// A tabela usuarios é plano de controle (sem GQF/RLS) — o filtro por tenant
/// é explícito aqui, sempre.
/// </summary>
[ApiController]
[Route("api/equipe")]
[Authorize]
[Produces("application/json")]
public class EquipeController(TechProDbContext db, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<EquipeMembroResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var tenantId = tenantProvider.TenantId
            ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

        var membros = await db.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Nome)
            .Select(u => new EquipeMembroResponse(u.Id, u.Nome, u.Email ?? ""))
            .ToListAsync();
        return Ok(membros);
    }
}
