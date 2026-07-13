using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Clientes;

public class AparelhoService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    /// <summary>Devolve null quando o cliente não pertence ao tenant (controller → 404).</summary>
    public async Task<AparelhoResponse?> CriarAsync(int clienteId, AparelhoRequest request)
    {
        // O GQF já filtra: cliente de outra empresa "não existe".
        if (!await db.Clientes.AnyAsync(c => c.Id == clienteId))
        {
            return null;
        }

        var aparelho = new Aparelho
        {
            TenantId = TenantId,
            ClienteId = clienteId,
            CriadoEm = DateTimeOffset.UtcNow,
            Marca = request.Marca.Trim(),
            Modelo = request.Modelo.Trim(),
        };
        Aplicar(aparelho, request);
        db.Aparelhos.Add(aparelho);
        await db.SaveChangesAsync();
        return ClienteService.ParaAparelhoResponse(aparelho);
    }

    public async Task<AparelhoResponse?> AtualizarAsync(int clienteId, int id, AparelhoRequest request)
    {
        var aparelho = await db.Aparelhos
            .SingleOrDefaultAsync(a => a.Id == id && a.ClienteId == clienteId);
        if (aparelho is null)
        {
            return null;
        }

        Aplicar(aparelho, request);
        await db.SaveChangesAsync();
        return ClienteService.ParaAparelhoResponse(aparelho);
    }

    public async Task<bool> DesativarAsync(int clienteId, int id)
    {
        var aparelho = await db.Aparelhos
            .SingleOrDefaultAsync(a => a.Id == id && a.ClienteId == clienteId);
        if (aparelho is null)
        {
            return false;
        }

        aparelho.Ativo = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static void Aplicar(Aparelho aparelho, AparelhoRequest request)
    {
        aparelho.Marca = request.Marca.Trim();
        aparelho.Modelo = request.Modelo.Trim();
        aparelho.Imei = Normalizar(request.Imei);
        aparelho.SenhaDesbloqueio = Normalizar(request.SenhaDesbloqueio);
        aparelho.Observacoes = Normalizar(request.Observacoes);
        aparelho.Ativo = request.Ativo;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
