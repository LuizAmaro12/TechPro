using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Shared.Auth;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
[Produces("application/json")]
public class AuthController(
    AuthService authService,
    IValidator<RegistrarRequest> validadorRegistrar,
    IValidator<LoginRequest> validadorLogin) : ControllerBase
{
    private const string NomeCookieRefresh = "techpro_refresh";

    [HttpPost("registrar")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistrarRequest requisicao)
    {
        var validacao = await validadorRegistrar.ValidateAsync(requisicao);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await authService.RegistrarAsync(requisicao, TipoCliente.Web);
        if (resultado.EmailJaCadastrado)
        {
            return Problem(
                title: "Este e-mail já está cadastrado.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (resultado.Tokens is null)
        {
            foreach (var erro in resultado.Erros)
            {
                ModelState.AddModelError(nameof(requisicao.Senha), erro);
            }

            return ValidationProblem(ModelState);
        }

        GravarCookieRefresh(resultado.Tokens);
        return Created("/api/auth/me", resultado.Tokens.Resposta);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest requisicao)
    {
        var validacao = await validadorLogin.ValidateAsync(requisicao);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var tokens = await authService.LoginAsync(requisicao, TipoCliente.Web);
        if (tokens is null)
        {
            // Mensagem única para e-mail inexistente, senha errada e lockout:
            // a resposta não pode servir de oráculo de e-mails cadastrados.
            return Problem(
                title: "E-mail ou senha inválidos.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        GravarCookieRefresh(tokens);
        return Ok(tokens.Resposta);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var cookie = Request.Cookies[NomeCookieRefresh];
        var tokens = string.IsNullOrEmpty(cookie) ? null : await authService.RefreshAsync(cookie);
        if (tokens is null)
        {
            RemoverCookieRefresh();
            return Problem(
                title: "Sessão expirada. Entre novamente.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        GravarCookieRefresh(tokens);
        return Ok(tokens.Resposta);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(Request.Cookies[NomeCookieRefresh]);
        RemoverCookieRefresh();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me()
    {
        var perfil = await authService.MeAsync(User);
        return perfil is null ? NotFound() : Ok(perfil);
    }

    private void GravarCookieRefresh(TokensEmitidos tokens) =>
        Response.Cookies.Append(NomeCookieRefresh, tokens.RefreshTokenPuro, OpcoesCookie(tokens.RefreshExpiraEm));

    private void RemoverCookieRefresh() =>
        Response.Cookies.Delete(NomeCookieRefresh, OpcoesCookie(expiraEm: null));

    // Path restrito a /api/auth: o cookie só viaja para refresh/logout, nunca
    // junto das requisições de dados — o access token em memória cobre essas.
    private static CookieOptions OpcoesCookie(DateTimeOffset? expiraEm) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/api/auth",
        Expires = expiraEm,
    };
}
