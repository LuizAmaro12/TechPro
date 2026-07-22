using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Resultado de uma emissão de tokens: a resposta pública + o refresh token
/// puro (vai só para o cookie httpOnly, nunca para o corpo da resposta).
/// </summary>
public sealed record TokensEmitidos(
    AuthResponse Resposta,
    string RefreshTokenPuro,
    DateTimeOffset RefreshExpiraEm,
    Guid RefreshTokenId);

public sealed record RegistroResultado(
    TokensEmitidos? Tokens,
    bool EmailJaCadastrado,
    IReadOnlyList<string> Erros)
{
    public static RegistroResultado Ok(TokensEmitidos tokens) => new(tokens, false, []);
    public static RegistroResultado Conflito() => new(null, true, []);
    public static RegistroResultado Falha(IEnumerable<string> erros) => new(null, false, [.. erros]);
}

public class AuthService(
    TechProDbContext db,
    UserManager<Usuario> userManager,
    SignInManager<Usuario> signInManager,
    TokenService tokenService)
{
    // Duração do refresh por tipo de cliente (seção 8 do doc de stack):
    // web 7 dias; o app mobile do técnico (Fase 2) tolera janelas offline longas.
    private static readonly TimeSpan DuracaoRefreshWeb = TimeSpan.FromDays(7);
    private static readonly TimeSpan DuracaoRefreshMobile = TimeSpan.FromDays(90);

    /// <summary>
    /// Cadastro público: cria Empresa (o tenant) + primeiro usuário como gestor
    /// numa única transação — ou nasce tudo, ou não nasce nada.
    /// </summary>
    public async Task<RegistroResultado> RegistrarAsync(RegistrarRequest requisicao, TipoCliente tipoCliente)
    {
        var email = requisicao.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return RegistroResultado.Conflito();
        }

        await using var transacao = await db.Database.BeginTransactionAsync();

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            Nome = requisicao.NomeEmpresa.Trim(),
            Slug = await GerarSlugUnicoAsync(requisicao.NomeEmpresa.Trim()),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            TenantId = empresa.Id,
            Nome = requisicao.Nome.Trim(),
            UserName = email,
            Email = email,
            EmailConfirmed = true, // MVP sem verificação de e-mail (fase 1).
            CriadoEm = DateTimeOffset.UtcNow,
        };

        var criacao = await userManager.CreateAsync(usuario, requisicao.Senha);
        if (!criacao.Succeeded)
        {
            return criacao.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName")
                ? RegistroResultado.Conflito()
                : RegistroResultado.Falha(criacao.Errors.Select(e => e.Description));
        }

        var papel = await userManager.AddToRoleAsync(usuario, Papeis.Gestor);
        if (!papel.Succeeded)
        {
            return RegistroResultado.Falha(papel.Errors.Select(e => e.Description));
        }

        var tokens = await EmitirTokensAsync(usuario, Papeis.Gestor, tipoCliente);
        await transacao.CommitAsync();
        return RegistroResultado.Ok(tokens);
    }

    /// <summary>
    /// Retorna null para QUALQUER falha (usuário inexistente, senha errada,
    /// lockout): a resposta não pode revelar se o e-mail existe.
    /// </summary>
    public async Task<TokensEmitidos?> LoginAsync(LoginRequest requisicao, TipoCliente tipoCliente)
    {
        var usuario = await userManager.FindByEmailAsync(requisicao.Email.Trim());
        // Membro desativado não entra — mesma resposta de credencial inválida,
        // para não revelar que a conta existe.
        if (usuario is null || !usuario.Ativo)
        {
            return null;
        }

        var resultado = await signInManager.CheckPasswordSignInAsync(
            usuario, requisicao.Senha, lockoutOnFailure: true);
        if (!resultado.Succeeded)
        {
            return null;
        }

        return await EmitirTokensAsync(usuario, await PapelDeAsync(usuario), tipoCliente);
    }

    /// <summary>
    /// Rotação: cada refresh revoga o token usado e emite um sucessor.
    /// Reapresentar um token JÁ ROTACIONADO é indício de roubo — nesse caso
    /// todos os tokens ativos do usuário são revogados (derruba a família).
    /// </summary>
    public async Task<TokensEmitidos?> RefreshAsync(string refreshTokenPuro)
    {
        var hash = TokenService.HashRefreshToken(refreshTokenPuro);
        var token = await db.RefreshTokens
            .Include(rt => rt.Usuario)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);
        if (token is null)
        {
            return null;
        }

        if (!token.EstaAtivo)
        {
            if (token.RevogadoEm is not null)
            {
                await db.RefreshTokens
                    .Where(rt => rt.UsuarioId == token.UsuarioId && rt.RevogadoEm == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevogadoEm, DateTimeOffset.UtcNow));
            }

            return null;
        }

        var tokens = await EmitirTokensAsync(
            token.Usuario, await PapelDeAsync(token.Usuario), token.TipoCliente);

        token.RevogadoEm = DateTimeOffset.UtcNow;
        token.SubstituidoPorId = tokens.RefreshTokenId;
        await db.SaveChangesAsync();

        return tokens;
    }

    public async Task LogoutAsync(string? refreshTokenPuro)
    {
        if (string.IsNullOrEmpty(refreshTokenPuro))
        {
            return;
        }

        var hash = TokenService.HashRefreshToken(refreshTokenPuro);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);
        if (token is { RevogadoEm: null })
        {
            token.RevogadoEm = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// A empresa é lida SEM Where explícito: quem filtra é o Global Query
    /// Filter (e o RLS no Postgres) a partir do tenant_id do JWT. Se o filtro
    /// não deixar a empresa passar, a resposta é nula — fail-closed.
    /// </summary>
    public async Task<MeResponse?> MeAsync(ClaimsPrincipal principal)
    {
        var papel = principal.FindFirstValue(TokenService.ClaimRole) ?? string.Empty;
        if (!Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var usuarioId))
        {
            return null;
        }

        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return null;
        }

        var empresa = await db.Empresas.SingleOrDefaultAsync();
        if (empresa is null || empresa.Id != usuario.TenantId)
        {
            return null;
        }

        return new MeResponse(
            usuario.Id,
            usuario.Nome,
            usuario.Email ?? string.Empty,
            papel,
            usuario.TenantId,
            new EmpresaResponse(empresa.Id, empresa.Nome));
    }

    private async Task<string> PapelDeAsync(Usuario usuario) =>
        (await userManager.GetRolesAsync(usuario)).FirstOrDefault() ?? Papeis.Atendente;

    private async Task<TokensEmitidos> EmitirTokensAsync(Usuario usuario, string papel, TipoCliente tipoCliente)
    {
        var (accessToken, expiraEm) = tokenService.GerarAccessToken(usuario, papel);
        var (refreshPuro, refreshHash) = tokenService.GerarRefreshToken();
        var agora = DateTimeOffset.UtcNow;

        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TenantId = usuario.TenantId,
            TokenHash = refreshHash,
            TipoCliente = tipoCliente,
            CriadoEm = agora,
            ExpiraEm = agora + (tipoCliente == TipoCliente.Mobile ? DuracaoRefreshMobile : DuracaoRefreshWeb),
        };
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync();

        var resposta = new AuthResponse(
            accessToken,
            expiraEm,
            new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email ?? string.Empty, papel, usuario.TenantId));

        return new TokensEmitidos(resposta, refreshPuro, refresh.ExpiraEm, refresh.Id);
    }

    /// <summary>
    /// Slug único global para a URL pública de agendamento. O filtro da
    /// Empresa é por Id == tenant corrente e no cadastro ainda não há tenant,
    /// então a checagem de unicidade ignora o filtro de propósito.
    /// </summary>
    private async Task<string> GerarSlugUnicoAsync(string nomeEmpresa)
    {
        var slugBase = GeradorDeSlug.Gerar(nomeEmpresa);
        var slug = slugBase;
        var sufixo = 2;
        while (await db.Empresas.IgnoreQueryFilters().AnyAsync(e => e.Slug == slug))
        {
            slug = $"{slugBase}-{sufixo++}";
        }

        return slug;
    }
}
