using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Compõe e despacha as notificações essenciais (módulo 9). Disparo automático
/// e síncrono nos eventos (o adaptador log mantém dev/teste sem envio real);
/// respeita o consentimento LGPD do cliente; grava um registro de auditoria
/// por canal. Falha de provedor externo nunca derruba a ação que disparou.
/// </summary>
public class ComunicacaoService(
    TechProDbContext db,
    ITenantProvider tenantProvider,
    IEnumerable<ICanalNotificacao> canais,
    IConfiguration configuracao,
    ILogger<ComunicacaoService> logger)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Notificação sem tenant resolvido.");

    private sealed record Destinatario(
        int? ClienteId, string Nome, string? Telefone, string? Email, bool Consentiu);

    /// <summary>
    /// Executa uma notificação sem deixar erro (provedor, banco) derrubar a
    /// ação que a disparou. Todos os gatilhos passam por aqui.
    /// </summary>
    public async Task ProtegerAsync(Func<Task> notificacao)
    {
        try
        {
            await notificacao();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notificação falhou — a ação principal foi preservada.");
        }
    }

    // --- Gatilhos por evento --------------------------------------------------------

    public async Task NotificarAgendamentoConfirmadoAsync(int agendamentoId)
    {
        var ag = await db.Agendamentos.Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == agendamentoId);
        if (ag is null)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        var quando = $"{ag.Data:dd/MM} às {ag.HoraInicio:hh\\:mm}";
        await DespacharAsync(
            await DestinatarioDoAgendamentoAsync(ag),
            TipoEventoComunicacao.AgendamentoConfirmado,
            contexto: ContextoAgendamento(ag, loja, quando),
            agendamentoId: ag.Id, ordemId: null, clienteId: ag.ClienteId);
    }

    public async Task NotificarLembreteAgendamentoAsync(int agendamentoId)
    {
        var ag = await db.Agendamentos.Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == agendamentoId);
        // Só lembra se o agendamento ainda está de pé (não cancelado/check-in).
        if (ag is null || ag.Status != StatusAgendamento.Agendado)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        var quando = $"{ag.Data:dd/MM} às {ag.HoraInicio:hh\\:mm}";
        await DespacharAsync(
            await DestinatarioDoAgendamentoAsync(ag),
            TipoEventoComunicacao.AgendamentoLembrete,
            contexto: ContextoAgendamento(ag, loja, quando),
            agendamentoId: ag.Id, ordemId: null, clienteId: ag.ClienteId);
    }

    public async Task NotificarOrdemServicoCriadaAsync(Guid ordemId)
    {
        var os = await CarregarOsAsync(ordemId);
        if (os is null)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        await DespacharAsync(
            DestinatarioDaOs(os),
            TipoEventoComunicacao.OrdemServicoCriada,
            contexto: await ContextoOsAsync(os, loja),
            agendamentoId: null, ordemId: os.Id, clienteId: os.ClienteId);
    }

    public async Task NotificarOrcamentoDisponivelAsync(Guid ordemId)
    {
        var os = await CarregarOsAsync(ordemId);
        var orcamento = await db.Orcamentos.FirstOrDefaultAsync(o => o.OrdemServicoId == ordemId);
        if (os is null || orcamento is null)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        var total = orcamento.ValorMaoDeObra + orcamento.ValorPecas - orcamento.Desconto;
        await DespacharAsync(
            DestinatarioDaOs(os),
            TipoEventoComunicacao.OrcamentoDisponivel,
            contexto: await ContextoOsAsync(os, loja, valor: Reais(total)),
            agendamentoId: null, ordemId: os.Id, clienteId: os.ClienteId);
    }

    public async Task NotificarOrcamentoRespostaAsync(Guid ordemId, bool aprovado)
    {
        var os = await CarregarOsAsync(ordemId);
        if (os is null)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        var evento = aprovado
            ? TipoEventoComunicacao.OrcamentoAprovado
            : TipoEventoComunicacao.OrcamentoRecusado;
        await DespacharAsync(
            DestinatarioDaOs(os), evento,
            contexto: await ContextoOsAsync(os, loja),
            agendamentoId: null, ordemId: os.Id, clienteId: os.ClienteId);
    }

    public async Task NotificarProntoParaRetiradaAsync(Guid ordemId)
    {
        var os = await CarregarOsAsync(ordemId);
        if (os is null)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        await DespacharAsync(
            DestinatarioDaOs(os),
            TipoEventoComunicacao.ProntoParaRetirada,
            contexto: await ContextoOsAsync(os, loja),
            agendamentoId: null, ordemId: os.Id, clienteId: os.ClienteId);
    }

    /// <summary>
    /// Pedido de avaliação — disparado só no evento de entrega (doc: gatilho
    /// após confirmação de entrega bem-sucedida, para não avaliar o contexto
    /// errado). Link é o mesmo acompanhamento público, onde o cliente avalia.
    /// </summary>
    public async Task NotificarPedidoAvaliacaoAsync(Guid ordemId)
    {
        var os = await CarregarOsAsync(ordemId);
        if (os is null)
        {
            return;
        }

        var loja = await NomeLojaAsync();
        await DespacharAsync(
            DestinatarioDaOs(os),
            TipoEventoComunicacao.PedidoAvaliacao,
            contexto: await ContextoOsAsync(os, loja),
            agendamentoId: null, ordemId: os.Id, clienteId: os.ClienteId);
    }

    // --- Contexto das variáveis de template -----------------------------------------

    private static Dictionary<string, string> ContextoAgendamento(
        Agendamento ag, string loja, string quando) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["cliente"] = ag.NomeContato,
            ["loja"] = loja,
            ["servico"] = ag.Servico?.Nome ?? "",
            ["data"] = quando,
        };

    private async Task<Dictionary<string, string>> ContextoOsAsync(
        OrdemServico os, string loja, string? valor = null) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["cliente"] = os.Cliente?.Nome ?? "",
            ["loja"] = loja,
            ["servico"] = os.Servico?.Nome ?? "",
            ["aparelho"] = DescricaoAparelho(os).Trim(),
            ["numero"] = os.Numero.ToString(),
            ["valor"] = valor ?? "",
            ["link"] = LinkAcompanhamento(os, await SlugLojaAsync(), curto: true),
        };

    // --- Núcleo de despacho ---------------------------------------------------------

    /// <summary>
    /// Texto efetivo do evento: o template da loja, se existir; senão o padrão
    /// embutido. Ausência = padrão — sem seed e sem migração de dados.
    /// </summary>
    private async Task<(string Assunto, string Corpo)> ComporAsync(
        TipoEventoComunicacao evento,
        IReadOnlyDictionary<string, string> contexto)
    {
        var (assuntoPadrao, corpoPadrao) = TemplatesPadrao.Para(evento);

        _templates ??= await db.TemplatesMensagem.ToDictionaryAsync(t => t.TipoEvento);
        var personalizado = _templates.GetValueOrDefault(evento);

        return (
            RenderizadorDeTemplate.Render(
                string.IsNullOrWhiteSpace(personalizado?.Assunto) ? assuntoPadrao : personalizado.Assunto,
                contexto),
            RenderizadorDeTemplate.Render(personalizado?.Corpo ?? corpoPadrao, contexto));
    }

    private Dictionary<TipoEventoComunicacao, TemplateMensagem>? _templates;

    private async Task DespacharAsync(
        Destinatario destinatario,
        TipoEventoComunicacao evento,
        IReadOnlyDictionary<string, string> contexto,
        int? agendamentoId,
        Guid? ordemId,
        int? clienteId)
    {
        var (assunto, corpo) = await ComporAsync(evento, contexto);

        var alvos = new List<(CanalNotificacao Canal, string Destino)>();
        if (!string.IsNullOrWhiteSpace(destinatario.Telefone))
        {
            alvos.Add((CanalNotificacao.WhatsApp, destinatario.Telefone!));
        }

        if (!string.IsNullOrWhiteSpace(destinatario.Email))
        {
            alvos.Add((CanalNotificacao.Email, destinatario.Email!));
        }

        foreach (var (canal, destino) in alvos)
        {
            var registro = new MensagemEnviada
            {
                TenantId = TenantId,
                ClienteId = clienteId,
                OrdemServicoId = ordemId,
                AgendamentoId = agendamentoId,
                Canal = canal,
                Destino = destino,
                TipoEvento = evento,
                Assunto = canal == CanalNotificacao.Email ? assunto : null,
                Corpo = corpo,
                CriadoEm = DateTimeOffset.UtcNow,
            };

            if (!destinatario.Consentiu)
            {
                // Gate 1 — LGPD: não envia, mas registra a supressão (auditoria).
                registro.Status = StatusMensagem.Suprimida;
            }
            else if (!await LojaQuerNotificarAsync(evento, canal))
            {
                // Gate 2 — a loja desligou este evento/canal nas preferências.
                registro.Status = StatusMensagem.Desativada;
            }
            else
            {
                var adaptador = canais.FirstOrDefault(c => c.Canal == canal);
                if (adaptador is null)
                {
                    registro.Status = StatusMensagem.Falhou;
                    registro.Erro = "Canal sem adaptador registrado.";
                }
                else
                {
                    var resultado = await adaptador.EnviarAsync(
                        destino, canal == CanalNotificacao.Email ? assunto : null, corpo);
                    registro.Status = !resultado.Sucesso
                        ? StatusMensagem.Falhou
                        : resultado.Simulado
                            ? StatusMensagem.Simulada
                            : StatusMensagem.Enviada;
                    registro.Erro = resultado.Erro;
                    registro.IdExterno = resultado.IdExterno;
                }
            }

            db.MensagensEnviadas.Add(registro);
        }

        if (alvos.Count == 0)
        {
            logger.LogWarning("Evento {Evento} sem canal (nem telefone nem e-mail).", evento);
        }

        await db.SaveChangesAsync();
    }

    // --- Auxiliares -----------------------------------------------------------------

    private async Task<OrdemServico?> CarregarOsAsync(Guid ordemId) =>
        await db.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Servico)
            .FirstOrDefaultAsync(o => o.Id == ordemId && o.DeletedAt == null);

    private static Destinatario DestinatarioDaOs(OrdemServico os) => new(
        os.ClienteId, os.Cliente!.Nome, os.Cliente.Telefone, os.Cliente.Email,
        os.Cliente.ConsentiuComunicacoes);

    private async Task<Destinatario> DestinatarioDoAgendamentoAsync(Agendamento ag)
    {
        // Agendamento avulso (sem cliente do CRM): consentimento implícito do
        // próprio pedido, feito com o contato informado.
        if (ag.ClienteId is not { } clienteId)
        {
            return new Destinatario(
                null, ag.NomeContato, ag.TelefoneContato, ag.EmailContato, Consentiu: true);
        }

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
        return new Destinatario(
            clienteId,
            ag.NomeContato,
            ag.TelefoneContato ?? cliente?.Telefone,
            ag.EmailContato ?? cliente?.Email,
            cliente?.ConsentiuComunicacoes ?? true);
    }

    private List<PreferenciaNotificacao>? _preferencias;

    /// <summary>
    /// Preferências da loja (evento × canal). Ausência de linha = ativo, então
    /// um tenant sem nenhuma configuração notifica tudo. Carregado uma vez por
    /// instância.
    /// </summary>
    private async Task<bool> LojaQuerNotificarAsync(TipoEventoComunicacao evento, CanalNotificacao canal)
    {
        _preferencias ??= await db.PreferenciasNotificacao.ToListAsync();
        return _preferencias
            .FirstOrDefault(p => p.TipoEvento == evento && p.Canal == canal)?.Ativo ?? true;
    }

    private string? _loja;
    private string? _slug;

    /// <summary>Nome da loja (empresa do tenant), carregado uma vez por instância.</summary>
    private async Task<string> NomeLojaAsync() =>
        _loja ??= await db.Empresas.Select(e => e.Nome).FirstAsync();

    private async Task<string> SlugLojaAsync() =>
        _slug ??= await db.Empresas.Select(e => e.Slug).FirstAsync();

    private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

    private static string Reais(decimal valor) => valor.ToString("C2", Brasil);

    private static string DescricaoAparelho(OrdemServico os)
    {
        var aparelho = string.Join(" ", new[] { os.AparelhoMarca, os.AparelhoModelo }
            .Where(v => !string.IsNullOrWhiteSpace(v)));
        return aparelho.Length > 0 ? $"{aparelho} " : "aparelho ";
    }

    private string LinkAcompanhamento(OrdemServico os, string slug, bool curto = false)
    {
        var url = $"{UrlBase()}/acompanhar/{slug}/{os.CodigoAcompanhamento}";
        return curto ? url : $"Acompanhe por aqui: {url}";
    }

    private string UrlBase() =>
        (configuracao["Comunicacao:UrlBasePublica"]
         ?? configuracao["Cors:FrontendOrigin"]
         ?? "http://localhost:3000").TrimEnd('/');
}
