using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Rota pública de agendamento (sem login): resolve o tenant pelo slug da
/// loja e o fixa no <see cref="TenantAmbiente"/> — a partir daí GQF e RLS
/// isolam a requisição normalmente. Rate limiting por IP conforme a seção
/// de segurança do doc de stack.
/// </summary>
[ApiController]
[Route("api/publico/{slug}")]
[AllowAnonymous]
[EnableRateLimiting("publico")]
[Produces("application/json")]
public class PublicoController(
    TechProDbContext db,
    TenantAmbiente tenantAmbiente,
    AgendamentoService agendamentos,
    DisponibilidadeService disponibilidade,
    IValidator<AgendamentoPublicoRequest> validador) : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType<LojaPublicaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Info(string slug)
    {
        var empresa = await ResolverLojaAsync(slug);
        if (empresa is null)
        {
            return NotFound();
        }

        var servicos = await db.Servicos
            .Where(s => s.Ativo && s.AgendavelOnline)
            .OrderBy(s => s.Nome)
            .Select(s => new ServicoPublicoResponse(
                s.Id, s.Nome, s.Categoria, s.PrecoBase, s.DuracaoEstimadaMinutos,
                s.PrazoMedioDias, s.ExigeDiagnostico))
            .ToListAsync();

        return Ok(new LojaPublicaResponse(
            empresa.Nome,
            empresa.Slug,
            new LojaContatoResponse(
                empresa.Telefone, empresa.Email, empresa.Endereco, empresa.Politicas),
            servicos));
    }

    [HttpGet("disponibilidade")]
    [ProducesResponseType<DisponibilidadeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disponibilidade(
        string slug, [FromQuery] int servicoId, [FromQuery] DateOnly data)
    {
        if (await ResolverLojaAsync(slug) is null)
        {
            return NotFound();
        }

        var resultado = await disponibilidade.CalcularAsync(servicoId, data, somenteAgendaveisOnline: true);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(resultado.Valor);
    }

    [HttpPost("agendamentos")]
    [ProducesResponseType<AgendamentoPublicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Criar(string slug, AgendamentoPublicoRequest request)
    {
        var empresa = await ResolverLojaAsync(slug);
        if (empresa is null)
        {
            return NotFound();
        }

        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await agendamentos.CriarPublicoAsync(request, empresa.Nome);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/publico/{slug}/agendamentos/{resultado.Valor!.Id}", resultado.Valor);
    }

    /// <summary>
    /// A busca por slug ignora o GQF da Empresa de propósito: ainda não há
    /// tenant. Achou → fixa o tenant da requisição; não achou → 404.
    /// </summary>
    private async Task<Empresa?> ResolverLojaAsync(string slug)
    {
        var empresa = await db.Empresas
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => e.Slug == slug);
        if (empresa is not null)
        {
            tenantAmbiente.TenantIdFixado = empresa.Id;
        }

        return empresa;
    }
}
