using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Configuração da agenda: horários de funcionamento, bloqueios e o slug
/// público da loja.
/// </summary>
public class AgendaService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    // --- Horários de funcionamento -------------------------------------------

    public async Task<List<HorarioFuncionamentoDia>> ListarHorariosAsync()
    {
        var existentes = await db.HorariosFuncionamento.ToListAsync();
        return Enumerable.Range(0, 7)
            .Select(dia => existentes.FirstOrDefault(h => h.DiaSemana == dia) is { } h
                ? new HorarioFuncionamentoDia(dia, h.Ativo, h.Abertura, h.Fechamento, h.IntervaloInicio, h.IntervaloFim)
                : new HorarioFuncionamentoDia(dia, false, null, null, null, null))
            .ToList();
    }

    public async Task<List<HorarioFuncionamentoDia>> SalvarHorariosAsync(HorariosFuncionamentoRequest request)
    {
        var existentes = await db.HorariosFuncionamento.ToListAsync();
        foreach (var dia in request.Dias)
        {
            var registro = existentes.FirstOrDefault(h => h.DiaSemana == dia.DiaSemana);
            if (registro is null)
            {
                registro = new HorarioFuncionamento { TenantId = TenantId, DiaSemana = dia.DiaSemana };
                db.HorariosFuncionamento.Add(registro);
            }

            registro.Ativo = dia.Ativo;
            // Dia inativo mantém 00:00 — os campos não são lidos com Ativo=false.
            registro.Abertura = dia.Abertura ?? default;
            registro.Fechamento = dia.Fechamento ?? default;
            registro.IntervaloInicio = dia.IntervaloInicio;
            registro.IntervaloFim = dia.IntervaloFim;
        }

        await db.SaveChangesAsync();
        return await ListarHorariosAsync();
    }

    // --- Bloqueios -------------------------------------------------------------

    public async Task<List<BloqueioResponse>> ListarBloqueiosAsync(DateOnly? deData, DateOnly? ateData)
    {
        var query = db.BloqueiosAgenda.AsQueryable();
        if (deData is { } de)
        {
            query = query.Where(b => b.Data >= de);
        }

        if (ateData is { } ate)
        {
            query = query.Where(b => b.Data <= ate);
        }

        return await query
            .OrderBy(b => b.Data).ThenBy(b => b.HoraInicio)
            .Select(b => new BloqueioResponse(b.Id, b.Data, b.HoraInicio, b.HoraFim, b.Motivo))
            .ToListAsync();
    }

    public async Task<BloqueioResponse> CriarBloqueioAsync(BloqueioRequest request)
    {
        var bloqueio = new BloqueioAgenda
        {
            TenantId = TenantId,
            Data = request.Data,
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim,
            Motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim(),
        };
        db.BloqueiosAgenda.Add(bloqueio);
        await db.SaveChangesAsync();
        return new BloqueioResponse(bloqueio.Id, bloqueio.Data, bloqueio.HoraInicio, bloqueio.HoraFim, bloqueio.Motivo);
    }

    public async Task<bool> RemoverBloqueioAsync(int id)
    {
        var bloqueio = await db.BloqueiosAgenda.FirstOrDefaultAsync(b => b.Id == id);
        if (bloqueio is null)
        {
            return false;
        }

        db.BloqueiosAgenda.Remove(bloqueio);
        await db.SaveChangesAsync();
        return true;
    }

    // --- Configurações (slug) ---------------------------------------------------

    public async Task<ConfiguracaoAgendaResponse> ObterConfiguracaoAsync()
    {
        // O GQF da Empresa é Id == tenant corrente: Single é seguro aqui.
        var empresa = await db.Empresas.SingleAsync();
        return new ConfiguracaoAgendaResponse(empresa.Slug);
    }

    public async Task<CatalogoResultado<ConfiguracaoAgendaResponse>> AtualizarSlugAsync(ConfiguracaoAgendaRequest request)
    {
        var slug = request.Slug.Trim();
        var emUso = await db.Empresas
            .IgnoreQueryFilters() // unicidade de slug é global, entre todas as lojas
            .AnyAsync(e => e.Slug == slug && e.Id != TenantId);
        if (emUso)
        {
            return CatalogoResultado<ConfiguracaoAgendaResponse>.Falha(
                "Este endereço já está em uso por outra loja.");
        }

        var empresa = await db.Empresas.SingleAsync();
        empresa.Slug = slug;
        await db.SaveChangesAsync();
        return CatalogoResultado<ConfiguracaoAgendaResponse>.Ok(new ConfiguracaoAgendaResponse(empresa.Slug));
    }
}
