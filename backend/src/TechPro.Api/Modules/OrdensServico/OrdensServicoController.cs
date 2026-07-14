using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.OrdensServico;

[ApiController]
[Route("api/ordens-servico")]
[Authorize]
[Produces("application/json")]
public class OrdensServicoController(
    OrdemServicoService service,
    IValidator<OrdemServicoRequest> validadorCriacao,
    IValidator<OrdemServicoAtualizacaoRequest> validadorAtualizacao,
    IValidator<MudancaEtapaRequest> validadorEtapa) : ControllerBase
{
    private Guid? UsuarioId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet]
    [ProducesResponseType<List<OrdemServicoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] EtapaOrdemServico? etapa,
        [FromQuery] string? busca,
        [FromQuery] Guid? responsavelId,
        [FromQuery] bool incluirFinalizadas = false) =>
        Ok(await service.ListarAsync(etapa, busca, responsavelId, incluirFinalizadas));

    /// <summary>
    /// Sincronização por delta (contrato do app do técnico, Fase 2): tudo que
    /// mudou desde <paramref name="since"/>, incluindo lápides de soft-delete.
    /// </summary>
    [HttpGet("sync")]
    [ProducesResponseType<OrdensServicoSyncResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sincronizar([FromQuery] DateTimeOffset? since) =>
        Ok(await service.SincronizarAsync(since));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrdemServicoDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id) =>
        await service.ObterAsync(id) is { } ordem ? Ok(ordem) : NotFound();

    [HttpPost]
    [ProducesResponseType<OrdemServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(
        OrdemServicoRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? chaveIdempotencia)
    {
        var validacao = await validadorCriacao.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.CriarAsync(request, chaveIdempotencia, UsuarioId);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/ordens-servico/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<OrdemServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid id, OrdemServicoAtualizacaoRequest request)
    {
        var validacao = await validadorAtualizacao.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return TraduzirResultado(await service.AtualizarAsync(id, request));
    }

    [HttpPost("{id:guid}/etapa")]
    [ProducesResponseType<OrdemServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MudarEtapa(Guid id, MudancaEtapaRequest request)
    {
        var validacao = await validadorEtapa.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        return TraduzirResultado(await service.MudarEtapaAsync(id, request, UsuarioId));
    }

    private IActionResult TraduzirResultado(CatalogoResultado<OrdemServicoResponse>? resultado)
    {
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(resultado.Valor);
    }
}
