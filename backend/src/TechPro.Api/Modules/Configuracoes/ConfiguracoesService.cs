using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Comunicacao;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Configuracoes;

/// <summary>
/// Configurações da loja (módulo 13): dados cadastrais e preferências de
/// notificação. O slug e os horários seguem em <c>/api/agenda/*</c> — a tela de
/// configurações linka para lá em vez de duplicar a regra.
/// </summary>
public class ConfiguracoesService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    /// <summary>Todos os eventos × canais — a matriz que a UI edita.</summary>
    private static readonly TipoEventoComunicacao[] Eventos =
        Enum.GetValues<TipoEventoComunicacao>();

    private static readonly CanalNotificacao[] Canais = Enum.GetValues<CanalNotificacao>();

    public async Task<LojaResponse> ObterLojaAsync()
    {
        var empresa = await db.Empresas.SingleAsync();
        return new LojaResponse(
            empresa.Nome, empresa.Slug, empresa.Telefone,
            empresa.Email, empresa.Endereco, empresa.Politicas);
    }

    public async Task<LojaResponse> SalvarLojaAsync(LojaRequest request)
    {
        var empresa = await db.Empresas.SingleAsync();
        empresa.Nome = request.Nome.Trim();
        empresa.Telefone = Normalizar(request.Telefone);
        empresa.Email = Normalizar(request.Email);
        empresa.Endereco = Normalizar(request.Endereco);
        empresa.Politicas = Normalizar(request.Politicas);
        await db.SaveChangesAsync();
        return await ObterLojaAsync();
    }

    /// <summary>
    /// A matriz completa: o que não tem linha no banco vem como ativo (o
    /// default do sistema é notificar).
    /// </summary>
    public async Task<PreferenciasNotificacaoResponse> ObterPreferenciasAsync()
    {
        var salvas = await db.PreferenciasNotificacao.ToListAsync();
        var itens = (from evento in Eventos
                     from canal in Canais
                     select new PreferenciaItem(
                         evento,
                         canal,
                         salvas.FirstOrDefault(p => p.TipoEvento == evento && p.Canal == canal)
                             ?.Ativo ?? true))
            .ToList();
        return new PreferenciasNotificacaoResponse(itens);
    }

    public async Task<PreferenciasNotificacaoResponse> SalvarPreferenciasAsync(
        PreferenciasNotificacaoRequest request)
    {
        var salvas = await db.PreferenciasNotificacao.ToListAsync();
        foreach (var item in request.Itens)
        {
            var existente = salvas
                .FirstOrDefault(p => p.TipoEvento == item.TipoEvento && p.Canal == item.Canal);
            if (existente is null)
            {
                db.PreferenciasNotificacao.Add(new PreferenciaNotificacao
                {
                    TenantId = TenantId,
                    TipoEvento = item.TipoEvento,
                    Canal = item.Canal,
                    Ativo = item.Ativo,
                });
            }
            else
            {
                existente.Ativo = item.Ativo;
            }
        }

        await db.SaveChangesAsync();
        return await ObterPreferenciasAsync();
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
