using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Acompanhamento público da OS (módulo 1, Fase 1): o cliente abre o link
/// com slug + código opaco, sem login. O slug resolve o tenant (mesmo padrão
/// do agendamento público) e o código não enumerável localiza a OS já sob
/// GQF+RLS. A resposta expõe só status — nada de dados pessoais.
/// </summary>
[ApiController]
[Route("api/publico/{slug}/acompanhar")]
[AllowAnonymous]
[EnableRateLimiting("publico")]
[Produces("application/json")]
public class AcompanhamentoController(
    TechProDbContext db,
    TenantAmbiente tenantAmbiente) : ControllerBase
{
    [HttpGet("{codigo}")]
    [ProducesResponseType<AcompanhamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(string slug, string codigo)
    {
        var empresa = await db.Empresas
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => e.Slug == slug);
        if (empresa is null)
        {
            return NotFound();
        }

        tenantAmbiente.TenantIdFixado = empresa.Id;

        var ordem = await db.OrdensServico
            .Include(o => o.Servico)
            .FirstOrDefaultAsync(o => o.CodigoAcompanhamento == codigo && o.DeletedAt == null);
        if (ordem is null)
        {
            return NotFound();
        }

        return Ok(new AcompanhamentoResponse(
            empresa.Nome,
            ordem.Numero,
            ordem.Servico!.Nome,
            ordem.Etapa,
            ordem.PrazoEstimado,
            ordem.UpdatedAt));
    }
}
