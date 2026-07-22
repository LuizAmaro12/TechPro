using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Comunicacao;
using TechPro.Api.Modules.Configuracoes.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
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

    // --- Templates de mensagem por evento ------------------------------------------

    /// <summary>
    /// Texto efetivo de cada evento: o personalizado da loja, se houver; senão
    /// o padrão do catálogo — que é literalmente o mesmo texto que o despacho
    /// usa, então a tela nunca mostra algo diferente do que o cliente recebe.
    /// </summary>
    public async Task<TemplatesResponse> ObterTemplatesAsync()
    {
        var salvos = await db.TemplatesMensagem.ToDictionaryAsync(t => t.TipoEvento);

        var itens = Eventos.Select(evento =>
        {
            var (assuntoPadrao, corpoPadrao) = TemplatesPadrao.Para(evento);
            var salvo = salvos.GetValueOrDefault(evento);
            return new TemplateItem(
                evento,
                string.IsNullOrWhiteSpace(salvo?.Assunto) ? assuntoPadrao : salvo.Assunto,
                salvo?.Corpo ?? corpoPadrao,
                Personalizado: salvo is not null,
                VariaveisDeTemplate.Para(evento));
        }).ToList();

        return new TemplatesResponse(itens);
    }

    /// <summary>
    /// Salva as personalizações. Corpo vazio **remove** a personalização (volta
    /// ao padrão). Variável inexistente para o evento é rejeitada aqui — o erro
    /// é pego na configuração, nunca no texto que chega ao cliente.
    /// </summary>
    public async Task<CatalogoResultado<TemplatesResponse>> SalvarTemplatesAsync(
        TemplatesRequest request)
    {
        foreach (var item in request.Itens)
        {
            var permitidas = VariaveisDeTemplate.Para(item.TipoEvento);
            var usadas = RenderizadorDeTemplate.VariaveisUsadas(item.Assunto)
                .Concat(RenderizadorDeTemplate.VariaveisUsadas(item.Corpo))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var invalida = usadas.FirstOrDefault(v =>
                !permitidas.Contains(v, StringComparer.OrdinalIgnoreCase));
            if (invalida is not null)
            {
                return CatalogoResultado<TemplatesResponse>.Falha(
                    $"A variável {{{invalida}}} não existe em {item.TipoEvento}. "
                    + $"Disponíveis: {string.Join(", ", permitidas.Select(v => $"{{{v}}}"))}.");
            }
        }

        var salvos = await db.TemplatesMensagem.ToListAsync();
        foreach (var item in request.Itens)
        {
            var existente = salvos.FirstOrDefault(t => t.TipoEvento == item.TipoEvento);
            var corpo = Normalizar(item.Corpo);

            if (corpo is null)
            {
                if (existente is not null)
                {
                    db.TemplatesMensagem.Remove(existente);
                }

                continue;
            }

            if (existente is null)
            {
                db.TemplatesMensagem.Add(new TemplateMensagem
                {
                    TenantId = TenantId,
                    TipoEvento = item.TipoEvento,
                    Assunto = Normalizar(item.Assunto),
                    Corpo = corpo,
                    AtualizadoEm = DateTimeOffset.UtcNow,
                });
            }
            else
            {
                existente.Assunto = Normalizar(item.Assunto);
                existente.Corpo = corpo;
                existente.AtualizadoEm = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return CatalogoResultado<TemplatesResponse>.Ok(await ObterTemplatesAsync());
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
