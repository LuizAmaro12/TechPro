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
        var (inicio, fim, erro) = ResolverPeriodo(de, ate);
        return erro ?? Ok(await service.ObterAsync(inicio, fim));
    }

    /// <summary>
    /// Margem realizada (Fase 2): OS entregues no período. Visão de competência,
    /// diferente do faturamento acima, que é caixa recebido.
    /// </summary>
    [HttpGet("rentabilidade")]
    [ProducesResponseType<RentabilidadeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Rentabilidade(
        [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        var (inicio, fim, erro) = ResolverPeriodo(de, ate);
        return erro ?? Ok(await service.ObterRentabilidadeAsync(inicio, fim));
    }

    /// <summary>Sem período informado: mês corrente.</summary>
    private (DateOnly Inicio, DateOnly Fim, IActionResult? Erro) ResolverPeriodo(
        DateOnly? de, DateOnly? ate)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = de ?? new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = ate ?? hoje;

        return fim < inicio
            ? (inicio, fim, Problem(
                title: "A data final deve ser igual ou posterior à inicial.",
                statusCode: StatusCodes.Status400BadRequest))
            : (inicio, fim, null);
    }
}
