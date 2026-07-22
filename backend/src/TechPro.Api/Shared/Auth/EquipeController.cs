using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Membros da loja. A leitura fica aberta a qualquer papel (o select de
/// responsável técnico precisa dela); toda **escrita** é do gestor.
/// A tabela usuarios é plano de controle (sem GQF/RLS) — o filtro por tenant
/// é explícito no service, sempre.
/// </summary>
[ApiController]
[Route("api/equipe")]
[Authorize]
[Produces("application/json")]
public class EquipeController(EquipeService service) : ControllerBase
{
    private Guid? UsuarioId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet]
    [ProducesResponseType<List<EquipeMembroResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInativos = false) =>
        Ok(await service.ListarAsync(incluirInativos));

    [HttpPost]
    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<EquipeMembroResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(NovoMembroRequest request)
    {
        var resultado = await service.CriarAsync(request);
        return resultado.Erro is not null
            ? Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest)
            : Created($"/api/equipe/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<EquipeMembroResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarMembroRequest request) =>
        Traduzir(await service.AtualizarAsync(id, request));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Politicas.Gestao)]
    [ProducesResponseType<EquipeMembroResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(Guid id) =>
        Traduzir(await service.DesativarAsync(id, UsuarioId));

    private IActionResult Traduzir(CatalogoResultado<EquipeMembroResponse>? resultado)
    {
        if (resultado is null)
        {
            return NotFound();
        }

        return resultado.Erro is not null
            ? Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest)
            : Ok(resultado.Valor);
    }
}

/// <summary>Histórico de ações sensíveis — só o gestor enxerga.</summary>
[ApiController]
[Route("api/auditoria")]
[Authorize(Policy = Politicas.Gestao)]
[Produces("application/json")]
public class AuditoriaController(AuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<RegistroAuditoriaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? entidade) =>
        Ok(await auditoria.ListarAsync(entidade));
}
