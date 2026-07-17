using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Modules.Configuracoes;

/// <summary>
/// Conta do próprio usuário (módulo 13). A leitura já existe em
/// <c>/api/auth/me</c>. Troca de e-mail fica fora da Fase 1: é o login, é único
/// globalmente e exige confirmação por e-mail (provedor ainda não ligado).
/// </summary>
[ApiController]
[Route("api/conta")]
[Authorize]
[Produces("application/json")]
public class ContaController(
    UserManager<Usuario> userManager,
    IValidator<ContaRequest> validadorConta,
    IValidator<TrocarSenhaRequest> validadorSenha) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Atualizar(ContaRequest request)
    {
        var validacao = await validadorConta.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var usuario = await UsuarioAtualAsync();
        if (usuario is null)
        {
            return Unauthorized();
        }

        usuario.Nome = request.Nome.Trim();
        var resultado = await userManager.UpdateAsync(usuario);
        return resultado.Succeeded
            ? NoContent()
            : Problem(
                title: string.Join(" ", resultado.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("senha")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrocarSenha(TrocarSenhaRequest request)
    {
        var validacao = await validadorSenha.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var usuario = await UsuarioAtualAsync();
        if (usuario is null)
        {
            return Unauthorized();
        }

        // ChangePasswordAsync já exige a senha atual — sem oráculo extra aqui.
        var resultado = await userManager.ChangePasswordAsync(
            usuario, request.SenhaAtual, request.NovaSenha);
        return resultado.Succeeded
            ? NoContent()
            : Problem(
                title: resultado.Errors.Any(e => e.Code == "PasswordMismatch")
                    ? "A senha atual não confere."
                    : string.Join(" ", resultado.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);
    }

    private async Task<Usuario?> UsuarioAtualAsync() =>
        User.FindFirstValue("sub") is { } id ? await userManager.FindByIdAsync(id) : null;
}
