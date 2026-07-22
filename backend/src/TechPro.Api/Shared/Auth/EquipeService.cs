using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Membros da loja (módulo 13 avançado). <c>usuarios</c> é plano de controle
/// (sem GQF/RLS), então **todo** acesso aqui filtra por TenantId explicitamente.
/// </summary>
public class EquipeService(
    TechProDbContext db,
    UserManager<Usuario> userManager,
    ITenantProvider tenantProvider,
    AuditoriaService auditoria)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    private static readonly string[] PapeisValidos = [Papeis.Gestor, Papeis.Tecnico, Papeis.Atendente];

    public async Task<List<EquipeMembroResponse>> ListarAsync(bool incluirInativos)
    {
        var query = db.Users.Where(u => u.TenantId == TenantId);
        if (!incluirInativos)
        {
            query = query.Where(u => u.Ativo);
        }

        var usuarios = await query.OrderBy(u => u.Nome).ToListAsync();

        var membros = new List<EquipeMembroResponse>();
        foreach (var usuario in usuarios)
        {
            membros.Add(new EquipeMembroResponse(
                usuario.Id, usuario.Nome, usuario.Email ?? "", await PapelDeAsync(usuario), usuario.Ativo));
        }

        return membros;
    }

    public async Task<CatalogoResultado<EquipeMembroResponse>> CriarAsync(NovoMembroRequest request)
    {
        if (!PapeisValidos.Contains(request.Papel))
        {
            return CatalogoResultado<EquipeMembroResponse>.Falha("Função inválida.");
        }

        var usuario = new Usuario
        {
            TenantId = TenantId,
            Nome = request.Nome.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            CriadoEm = DateTimeOffset.UtcNow,
        };

        var criacao = await userManager.CreateAsync(usuario, request.Senha);
        if (!criacao.Succeeded)
        {
            return CatalogoResultado<EquipeMembroResponse>.Falha(
                string.Join(" ", criacao.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(usuario, request.Papel);
        await auditoria.RegistrarESalvarAsync(
            "Membro adicionado", AreasAuditadas.Equipe, usuario.Id.ToString(),
            $"{usuario.Nome} ({request.Papel})");

        return CatalogoResultado<EquipeMembroResponse>.Ok(new EquipeMembroResponse(
            usuario.Id, usuario.Nome, usuario.Email ?? "", request.Papel, usuario.Ativo));
    }

    public async Task<CatalogoResultado<EquipeMembroResponse>?> AtualizarAsync(
        Guid id, AtualizarMembroRequest request)
    {
        if (!PapeisValidos.Contains(request.Papel))
        {
            return CatalogoResultado<EquipeMembroResponse>.Falha("Função inválida.");
        }

        var usuario = await BuscarDoTenantAsync(id);
        if (usuario is null)
        {
            return null;
        }

        var papelAtual = await PapelDeAsync(usuario);
        if (papelAtual == Papeis.Gestor && request.Papel != Papeis.Gestor
            && !await ExisteOutroGestorAsync(id))
        {
            return CatalogoResultado<EquipeMembroResponse>.Falha(
                "A loja precisa de pelo menos um gestor.");
        }

        usuario.Nome = request.Nome.Trim();
        if (papelAtual != request.Papel)
        {
            await userManager.RemoveFromRoleAsync(usuario, papelAtual);
            await userManager.AddToRoleAsync(usuario, request.Papel);
        }

        await userManager.UpdateAsync(usuario);
        await auditoria.RegistrarESalvarAsync(
            "Membro atualizado", AreasAuditadas.Equipe, usuario.Id.ToString(),
            $"{usuario.Nome} ({papelAtual} → {request.Papel})");

        return CatalogoResultado<EquipeMembroResponse>.Ok(new EquipeMembroResponse(
            usuario.Id, usuario.Nome, usuario.Email ?? "", request.Papel, usuario.Ativo));
    }

    public async Task<CatalogoResultado<EquipeMembroResponse>?> DesativarAsync(Guid id, Guid? quemPediu)
    {
        var usuario = await BuscarDoTenantAsync(id);
        if (usuario is null)
        {
            return null;
        }

        if (id == quemPediu)
        {
            return CatalogoResultado<EquipeMembroResponse>.Falha(
                "Você não pode desativar a própria conta.");
        }

        if (await PapelDeAsync(usuario) == Papeis.Gestor && !await ExisteOutroGestorAsync(id))
        {
            return CatalogoResultado<EquipeMembroResponse>.Falha(
                "A loja precisa de pelo menos um gestor.");
        }

        usuario.Ativo = false;
        await userManager.UpdateAsync(usuario);
        await auditoria.RegistrarESalvarAsync(
            "Membro desativado", AreasAuditadas.Equipe, usuario.Id.ToString(), usuario.Nome);

        return CatalogoResultado<EquipeMembroResponse>.Ok(new EquipeMembroResponse(
            usuario.Id, usuario.Nome, usuario.Email ?? "", await PapelDeAsync(usuario), false));
    }

    // --- Auxiliares ----------------------------------------------------------------

    private Task<Usuario?> BuscarDoTenantAsync(Guid id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId);

    private async Task<string> PapelDeAsync(Usuario usuario) =>
        (await userManager.GetRolesAsync(usuario)).FirstOrDefault() ?? Papeis.Atendente;

    /// <summary>
    /// Guarda do último gestor: sem isso a loja se tranca para fora das
    /// configurações, da equipe e do financeiro — sem caminho de volta.
    /// </summary>
    private async Task<bool> ExisteOutroGestorAsync(Guid exceto)
    {
        var ativos = await db.Users
            .Where(u => u.TenantId == TenantId && u.Ativo && u.Id != exceto)
            .ToListAsync();
        foreach (var usuario in ativos)
        {
            if (await PapelDeAsync(usuario) == Papeis.Gestor)
            {
                return true;
            }
        }

        return false;
    }
}
