using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.OrdensServico.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// O que acontece "em volta" da OS além do fluxo de etapas: comentários
/// internos da equipe e troca de responsável técnico com motivo. Ambos existem
/// para responder depois "por que isso foi feito assim e por quem".
/// </summary>
public class OrdemServicoInteracaoService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    // --- Comentários internos ------------------------------------------------------

    public async Task<List<ComentarioResponse>?> ListarComentariosAsync(Guid ordemId)
    {
        if (!await OrdemExisteAsync(ordemId))
        {
            return null;
        }

        return await CarregarComentariosAsync(ordemId);
    }

    public async Task<CatalogoResultado<ComentarioResponse>?> ComentarAsync(
        Guid ordemId, ComentarioRequest request, Guid? usuarioId)
    {
        if (!await OrdemExisteAsync(ordemId))
        {
            return null;
        }

        var comentario = new OrdemServicoComentario
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrdemServicoId = ordemId,
            Texto = request.Texto.Trim(),
            AutorUsuarioId = usuarioId,
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.OrdensServicoComentarios.Add(comentario);
        await db.SaveChangesAsync();

        var nomes = await ResolverNomesAsync([usuarioId]);
        return CatalogoResultado<ComentarioResponse>.Ok(ParaResponse(comentario, nomes));
    }

    /// <summary>Soft-delete: a lápide precisa chegar ao app do técnico pelo /sync.</summary>
    public async Task<bool> RemoverComentarioAsync(Guid ordemId, Guid comentarioId)
    {
        var comentario = await db.OrdensServicoComentarios.FirstOrDefaultAsync(c =>
            c.Id == comentarioId && c.OrdemServicoId == ordemId && c.DeletedAt == null);
        if (comentario is null)
        {
            return false;
        }

        comentario.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    internal async Task<List<ComentarioResponse>> CarregarComentariosAsync(Guid ordemId)
    {
        // Ordenação em memória: Sqlite (testes) não ordena DateTimeOffset.
        var comentarios = (await db.OrdensServicoComentarios
                .Where(c => c.OrdemServicoId == ordemId && c.DeletedAt == null)
                .ToListAsync())
            .OrderBy(c => c.CriadoEm)
            .ToList();

        var nomes = await ResolverNomesAsync(comentarios.Select(c => c.AutorUsuarioId));
        return comentarios.Select(c => ParaResponse(c, nomes)).ToList();
    }

    // --- Reatribuição de técnico ---------------------------------------------------

    /// <summary>
    /// Troca o responsável e grava a trilha append-only. Valida o técnico no
    /// tenant à mão porque <c>usuarios</c> é plano de controle e não tem Global
    /// Query Filter — mesma proteção anti-IDOR do PUT da OS.
    /// </summary>
    public async Task<CatalogoResultado<ReatribuicaoResponse>?> ReatribuirAsync(
        Guid ordemId, ReatribuicaoRequest request, Guid? usuarioId)
    {
        var ordem = await db.OrdensServico
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);
        if (ordem is null)
        {
            return null;
        }

        if (request.ResponsavelTecnicoId is { } novo &&
            !await db.Users.AnyAsync(u => u.Id == novo && u.TenantId == TenantId))
        {
            return CatalogoResultado<ReatribuicaoResponse>.Falha("Responsável técnico não encontrado.");
        }

        if (ordem.ResponsavelTecnicoId == request.ResponsavelTecnicoId)
        {
            return CatalogoResultado<ReatribuicaoResponse>.Falha("A OS já está com esse responsável.");
        }

        var troca = new OrdemServicoReatribuicao
        {
            TenantId = TenantId,
            OrdemServicoId = ordemId,
            DeUsuarioId = ordem.ResponsavelTecnicoId,
            ParaUsuarioId = request.ResponsavelTecnicoId,
            Motivo = request.Motivo.Trim(),
            PorUsuarioId = usuarioId,
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.OrdensServicoReatribuicoes.Add(troca);
        ordem.ResponsavelTecnicoId = request.ResponsavelTecnicoId;
        await db.SaveChangesAsync();

        var nomes = await ResolverNomesAsync(
            [troca.DeUsuarioId, troca.ParaUsuarioId, troca.PorUsuarioId]);
        return CatalogoResultado<ReatribuicaoResponse>.Ok(new ReatribuicaoResponse(
            troca.Id,
            troca.DeUsuarioId, Nome(nomes, troca.DeUsuarioId),
            troca.ParaUsuarioId, Nome(nomes, troca.ParaUsuarioId),
            troca.Motivo, Nome(nomes, troca.PorUsuarioId), troca.CriadoEm));
    }

    internal async Task<List<ReatribuicaoResponse>> CarregarReatribuicoesAsync(Guid ordemId)
    {
        var trocas = (await db.OrdensServicoReatribuicoes
                .Where(r => r.OrdemServicoId == ordemId)
                .ToListAsync())
            .OrderBy(r => r.CriadoEm)
            .ToList();

        var nomes = await ResolverNomesAsync(
            trocas.SelectMany(r => new[] { r.DeUsuarioId, r.ParaUsuarioId, r.PorUsuarioId }));

        return trocas.Select(r => new ReatribuicaoResponse(
            r.Id,
            r.DeUsuarioId,
            Nome(nomes, r.DeUsuarioId),
            r.ParaUsuarioId,
            Nome(nomes, r.ParaUsuarioId),
            r.Motivo,
            Nome(nomes, r.PorUsuarioId),
            r.CriadoEm)).ToList();
    }

    // --- Auxiliares ----------------------------------------------------------------

    private Task<bool> OrdemExisteAsync(Guid ordemId) =>
        db.OrdensServico.AnyAsync(o => o.Id == ordemId && o.DeletedAt == null);

    /// <summary>usuarios não tem GQF — o filtro por tenant é explícito.</summary>
    private async Task<Dictionary<Guid, string>> ResolverNomesAsync(IEnumerable<Guid?> ids)
    {
        var alvos = ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
        if (alvos.Count == 0)
        {
            return [];
        }

        return await db.Users
            .Where(u => alvos.Contains(u.Id) && u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.Nome);
    }

    private static string? Nome(Dictionary<Guid, string> nomes, Guid? id) =>
        id is { } valor && nomes.TryGetValue(valor, out var nome) ? nome : null;

    private static ComentarioResponse ParaResponse(
        OrdemServicoComentario c, Dictionary<Guid, string> nomes) =>
        new(c.Id, c.Texto, c.AutorUsuarioId, Nome(nomes, c.AutorUsuarioId), c.CriadoEm);
}
