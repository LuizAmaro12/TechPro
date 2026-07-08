using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class FornecedorService(TechProDbContext db, ITenantProvider tenantProvider)
{
    public enum Remocao { Removido, NaoEncontrado, EmUso }

    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<IReadOnlyList<FornecedorResponse>> ListarAsync() =>
        await db.Fornecedores
            .OrderBy(f => f.Nome)
            .Select(f => new FornecedorResponse(f.Id, f.Nome, f.Contato))
            .ToListAsync();

    public async Task<FornecedorResponse> CriarAsync(FornecedorRequest request)
    {
        var fornecedor = new Fornecedor
        {
            TenantId = TenantId,
            Nome = request.Nome.Trim(),
            Contato = Normalizar(request.Contato),
        };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync();
        return new FornecedorResponse(fornecedor.Id, fornecedor.Nome, fornecedor.Contato);
    }

    public async Task<FornecedorResponse?> AtualizarAsync(int id, FornecedorRequest request)
    {
        // O GQF já limita ao tenant atual: id de outra empresa "não existe".
        var fornecedor = await db.Fornecedores.SingleOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null)
        {
            return null;
        }

        fornecedor.Nome = request.Nome.Trim();
        fornecedor.Contato = Normalizar(request.Contato);
        await db.SaveChangesAsync();
        return new FornecedorResponse(fornecedor.Id, fornecedor.Nome, fornecedor.Contato);
    }

    public async Task<Remocao> RemoverAsync(int id)
    {
        var fornecedor = await db.Fornecedores.SingleOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null)
        {
            return Remocao.NaoEncontrado;
        }

        if (await db.Pecas.AnyAsync(p => p.FornecedorId == id))
        {
            return Remocao.EmUso;
        }

        db.Fornecedores.Remove(fornecedor);
        await db.SaveChangesAsync();
        return Remocao.Removido;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
