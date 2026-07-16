using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Financeiro.Dtos;

namespace TechPro.Api.Modules.Financeiro;

/// <summary>Relatório de caixa da loja (módulo 8).</summary>
[ApiController]
[Route("api/financeiro")]
[Authorize]
[Produces("application/json")]
public class FinanceiroRelatorioController(FinanceiroRelatorioService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FinanceiroRelatorioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Obter([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        // Sem período informado: mês corrente.
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = de ?? new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = ate ?? hoje;

        if (fim < inicio)
        {
            return Problem(
                title: "A data final deve ser igual ou posterior à inicial.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(await service.ObterAsync(inicio, fim));
    }
}
