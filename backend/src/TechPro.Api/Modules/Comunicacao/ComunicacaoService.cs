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
            assunto: $"Agendamento confirmado — {loja}",
            corpo: $"Olá, {ag.NomeContato}! Seu agendamento de {ag.Servico?.Nome} na {loja} "
                 + $"está confirmado para {quando}. Até lá!",
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
            assunto: $"Lembrete do seu agendamento — {loja}",
            corpo: $"Oi, {ag.NomeContato}! Passando para lembrar do seu agendamento de "
                 + $"{ag.Servico?.Nome} na {loja} em {quando}. Se precisar remarcar, é só avisar.",
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
            assunto: $"Recebemos seu aparelho — OS #{os.Numero} ({loja})",
            corpo: $"Olá, {os.Cliente!.Nome}! Abrimos a ordem de serviço #{os.Numero} para o seu "
                 + $"{DescricaoAparelho(os)}({os.Servico?.Nome}). {LinkAcompanhamento(os, await SlugLojaAsync())}",
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
            assunto: $"Orçamento da OS #{os.Numero} — {loja}",
            corpo: $"Olá, {os.Cliente!.Nome}! O orçamento do reparo do seu {DescricaoAparelho(os)}"
                 + $"ficou em {Reais(total)}. Você pode aprovar ou recusar por aqui: "
                 + $"{LinkAcompanhamento(os, await SlugLojaAsync(), curto: true)}",
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
        var (evento, assunto, corpo) = aprovado
            ? (TipoEventoComunicacao.OrcamentoAprovado,
               $"Orçamento aprovado — OS #{os.Numero}",
               $"Recebemos a aprovação do orçamento da OS #{os.Numero}. Já vamos seguir com o "
               + $"reparo do seu {DescricaoAparelho(os)}e avisamos quando estiver pronto!")
            : (TipoEventoComunicacao.OrcamentoRecusado,
               $"Orçamento recusado — OS #{os.Numero}",
               $"Registramos a recusa do orçamento da OS #{os.Numero}. Se quiser conversar sobre "
               + $"outras opções, é só falar com a {loja}.");
        await DespacharAsync(
            DestinatarioDaOs(os), evento, assunto, corpo,
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
            assunto: $"Seu aparelho está pronto! — OS #{os.Numero} ({loja})",
            corpo: $"Boa notícia, {os.Cliente!.Nome}! O reparo do seu {DescricaoAparelho(os)}"
                 + $"foi concluído e está pronto para retirada na {loja}. Te esperamos!",
            agendamentoId: null, ordemId: os.Id, clienteId: os.ClienteId);
    }

    // --- Núcleo de despacho ---------------------------------------------------------

    private async Task DespacharAsync(
        Destinatario destinatario,
        TipoEventoComunicacao evento,
        string assunto,
        string corpo,
        int? agendamentoId,
        Guid? ordemId,
        int? clienteId)
    {
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
