using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class ServicoService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<PaginaResponse<ServicoResponse>> ListarAsync(
        string? busca, string? categoria, bool incluirInativos, int pagina, int tamanhoPagina)
    {
        var query = db.Servicos
            .Include(s => s.Checklist)
            .Include(s => s.Pecas).ThenInclude(sp => sp.Peca)
            .AsQueryable();

        if (!incluirInativos)
        {
            query = query.Where(s => s.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(s => s.Nome.ToLower().Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var termoCategoria = categoria.Trim().ToLower();
            query = query.Where(s => s.Categoria != null && s.Categoria.ToLower() == termoCategoria);
        }

        var total = await query.CountAsync();
        var itens = await query
            .OrderBy(s => s.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return new PaginaResponse<ServicoResponse>(
            itens.Select(ParaResponse).ToList(), total, pagina, tamanhoPagina);
    }

    public async Task<ServicoResponse?> ObterAsync(int id)
    {
        var servico = await db.Servicos
            .Include(s => s.Checklist)
            .Include(s => s.Pecas).ThenInclude(sp => sp.Peca)
            .SingleOrDefaultAsync(s => s.Id == id);
        return servico is null ? null : ParaResponse(servico);
    }

    public async Task<CatalogoResultado<ServicoResponse>> CriarAsync(ServicoRequest request)
    {
        if (!await PecasExistemAsync(request.Pecas))
        {
            return CatalogoResultado<ServicoResponse>.Falha("Uma ou mais peças informadas não existem.");
        }

        var servico = new Servico
        {
            TenantId = TenantId,
            CriadoEm = DateTimeOffset.UtcNow,
            Nome = request.Nome.Trim(),
        };
        Aplicar(servico, request);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();
        return CatalogoResultado<ServicoResponse>.Ok((await ObterAsync(servico.Id))!);
    }

    public async Task<CatalogoResultado<ServicoResponse>?> AtualizarAsync(int id, ServicoRequest request)
    {
        var servico = await db.Servicos
            .Include(s => s.Checklist)
            .Include(s => s.Pecas)
            .SingleOrDefaultAsync(s => s.Id == id);
        if (servico is null)
        {
            return null;
        }

        if (!await PecasExistemAsync(request.Pecas))
        {
            return CatalogoResultado<ServicoResponse>.Falha("Uma ou mais peças informadas não existem.");
        }

        Aplicar(servico, request);
        await db.SaveChangesAsync();
        return CatalogoResultado<ServicoResponse>.Ok((await ObterAsync(id))!);
    }

    public async Task<bool> DesativarAsync(int id)
    {
        var servico = await db.Servicos.SingleOrDefaultAsync(s => s.Id == id);
        if (servico is null)
        {
            return false;
        }

        servico.Ativo = false;
        await db.SaveChangesAsync();
        return true;
    }

    // Substituição integral de checklist e peças: semântica de PUT, coleções
    // pequenas — mais simples e à prova de estados parciais do que um diff.
    private void Aplicar(Servico servico, ServicoRequest request)
    {
        servico.Nome = request.Nome.Trim();
        servico.Categoria = string.IsNullOrWhiteSpace(request.Categoria) ? null : request.Categoria.Trim();
        servico.PrecoBase = request.PrecoBase;
        servico.DuracaoEstimadaMinutos = request.DuracaoEstimadaMinutos;
        servico.PrazoMedioDias = request.PrazoMedioDias;
        servico.ExigeDiagnostico = request.ExigeDiagnostico;
        servico.AgendavelOnline = request.AgendavelOnline;
        servico.CapacidadeSimultanea = request.CapacidadeSimultanea;
        servico.SlaHoras = request.SlaHoras;
        servico.Ativo = request.Ativo;
        servico.Checklist.Clear();
        servico.Checklist.AddRange(request.Checklist.Select((descricao, indice) => new ServicoChecklistItem
        {
            TenantId = TenantId,
            Ordem = indice + 1,
            Descricao = descricao.Trim(),
        }));
        servico.Pecas.Clear();
        servico.Pecas.AddRange(request.Pecas.Select(p => new ServicoPeca
        {
            TenantId = TenantId,
            PecaId = p.PecaId,
            QuantidadePadrao = p.QuantidadePadrao,
        }));
    }

    // O GQF faz peça de outra empresa "não existir" aqui: referência cruzada
    // de tenant vira 400 de negócio, nunca um vínculo silencioso (IDOR).
    private async Task<bool> PecasExistemAsync(IReadOnlyList<ServicoPecaRequest> pecas)
    {
        if (pecas.Count == 0)
        {
            return true;
        }

        var ids = pecas.Select(p => p.PecaId).Distinct().ToList();
        return await db.Pecas.CountAsync(p => ids.Contains(p.Id)) == ids.Count;
    }

    private static ServicoResponse ParaResponse(Servico s) => new(
        s.Id, s.Nome, s.Categoria, s.PrecoBase, s.DuracaoEstimadaMinutos, s.PrazoMedioDias,
        s.ExigeDiagnostico, s.AgendavelOnline, s.CapacidadeSimultanea, s.SlaHoras, s.Ativo,
        s.Checklist.OrderBy(i => i.Ordem).Select(i => i.Descricao).ToList(),
        s.Pecas.Select(p => new ServicoPecaResponse(p.PecaId, p.Peca?.Nome ?? "", p.QuantidadePadrao)).ToList());
}
