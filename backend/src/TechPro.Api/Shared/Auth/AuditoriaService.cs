using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Registro das ações sensíveis. Enfileira no contexto — quem chamou decide a
/// fronteira da transação, como no razão de estoque —, exceto no
/// <see cref="RegistrarESalvarAsync"/>, usado quando a ação já foi persistida.
/// </summary>
public class AuditoriaService(
    TechProDbContext db,
    ITenantProvider tenantProvider,
    IHttpContextAccessor contexto)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Auditoria sem tenant resolvido.");

    public void Registrar(string acao, string entidade, string? entidadeId = null, string? detalhe = null)
    {
        var usuario = contexto.HttpContext?.User;
        db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            TenantId = TenantId,
            UsuarioId = Guid.TryParse(usuario?.FindFirstValue("sub"), out var id) ? id : null,
            UsuarioNome = usuario?.FindFirstValue("nome")
                ?? usuario?.FindFirstValue(ClaimTypes.Email)
                ?? usuario?.FindFirstValue("email")
                ?? "sistema",
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Detalhe = detalhe,
            CriadoEm = DateTimeOffset.UtcNow,
        });
    }

    public async Task RegistrarESalvarAsync(
        string acao, string entidade, string? entidadeId = null, string? detalhe = null)
    {
        Registrar(acao, entidade, entidadeId, detalhe);
        await db.SaveChangesAsync();
    }

    public async Task<List<RegistroAuditoriaResponse>> ListarAsync(string? entidade)
    {
        var query = db.RegistrosAuditoria.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidade))
        {
            query = query.Where(r => r.Entidade == entidade);
        }

        // Id decrescente: cresce com o tempo e traduz em qualquer provedor
        // (Sqlite dos testes não ordena DateTimeOffset no servidor).
        var registros = await query.OrderByDescending(r => r.Id).Take(200).ToListAsync();
        return registros
            .Select(r => new RegistroAuditoriaResponse(
                r.Id, r.UsuarioNome, r.Acao, r.Entidade, r.EntidadeId, r.Detalhe, r.CriadoEm))
            .ToList();
    }
}

public record RegistroAuditoriaResponse(
    int Id,
    string UsuarioNome,
    string Acao,
    string Entidade,
    string? EntidadeId,
    string? Detalhe,
    DateTimeOffset CriadoEm);

/// <summary>Áreas auditadas — constantes para não espalhar string solta.</summary>
public static class AreasAuditadas
{
    public const string Equipe = "Equipe";
    public const string Lgpd = "LGPD";
    public const string Configuracoes = "Configurações";
}
