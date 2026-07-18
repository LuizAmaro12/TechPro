using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Modules.Clientes;

/// <summary>
/// Operacionaliza os direitos LGPD do cliente final (módulo 14): exportação
/// (portabilidade) e exclusão por **anonimização** — os dados pessoais viram
/// marcadores genéricos, mas o registro estrutural da OS (histórico financeiro
/// e de estoque) é preservado (seção 16 do doc de stack). Tudo sob GQF — não há
/// como tocar dados de outra empresa.
/// </summary>
public class LgpdService(TechProDbContext db, ClienteService clientes)
{
    /// <summary>Exportação: todos os dados pessoais do cliente vinculados ao tenant.</summary>
    public async Task<DadosPessoaisResponse?> ExportarAsync(int clienteId)
    {
        var detalhe = await clientes.ObterAsync(clienteId);
        if (detalhe is null)
        {
            return null;
        }

        var agendamentos = await db.Agendamentos
            .Where(a => a.ClienteId == clienteId)
            .OrderBy(a => a.Data).ThenBy(a => a.HoraInicio)
            .Select(a => new AgendamentoExportado(
                a.Id, a.Servico!.Nome, a.Data, a.HoraInicio, a.Status.ToString(),
                a.NomeContato, a.TelefoneContato, a.EmailContato))
            .ToListAsync();

        var ordens = (await db.OrdensServico
                .Where(o => o.ClienteId == clienteId && o.DeletedAt == null)
                .Select(o => new
                {
                    o.Numero, ServicoNome = o.Servico!.Nome, o.Etapa,
                    o.AparelhoMarca, o.AparelhoModelo, o.CriadoEm,
                })
                .ToListAsync())
            .OrderByDescending(o => o.CriadoEm)
            .Select(o => new OrdemServicoExportada(
                o.Numero, o.ServicoNome, o.Etapa.ToString(),
                o.AparelhoMarca, o.AparelhoModelo, o.CriadoEm))
            .ToList();

        var mensagens = (await db.MensagensEnviadas
                .Where(m => m.ClienteId == clienteId)
                .Select(m => new
                {
                    m.Canal, m.TipoEvento, m.Destino, m.Status, m.CriadoEm,
                })
                .ToListAsync())
            .OrderByDescending(m => m.CriadoEm)
            .Select(m => new MensagemExportada(
                m.Canal.ToString(), m.TipoEvento.ToString(), m.Destino,
                m.Status.ToString(), m.CriadoEm))
            .ToList();

        return new DadosPessoaisResponse(detalhe, agendamentos, ordens, mensagens,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Anonimização (direito de exclusão): substitui os dados pessoais ligados
    /// ao cliente por marcadores genéricos — na própria linha e nos snapshots
    /// de aparelhos, agendamentos e mensagens. Preserva OS/pagamentos/orçamentos
    /// (integridade). Irreversível e idempotente.
    /// </summary>
    public async Task<ClienteDetalheResponse?> AnonimizarAsync(int clienteId)
    {
        var cliente = await db.Clientes
            .FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente is null)
        {
            return null;
        }

        if (cliente.AnonimizadoEm is not null)
        {
            return await clientes.ObterAsync(clienteId); // já anonimizado — no-op
        }

        var agora = DateTimeOffset.UtcNow;
        const string marcador = "anonimizado";

        cliente.Nome = $"Cliente anonimizado #{cliente.Id}";
        cliente.Telefone = marcador;
        cliente.Email = null;
        cliente.Cpf = null;
        cliente.Endereco = null;
        cliente.Observacoes = null;
        cliente.Vip = false;
        cliente.ConsentiuComunicacoes = false;
        cliente.ConsentimentoEm = null;
        cliente.Ativo = false;
        cliente.AnonimizadoEm = agora;

        // Aparelhos: IMEI e senha de desbloqueio são rastreáveis/sensíveis.
        foreach (var aparelho in await db.Aparelhos.Where(a => a.ClienteId == clienteId).ToListAsync())
        {
            aparelho.Imei = null;
            aparelho.SenhaDesbloqueio = null;
            aparelho.Observacoes = null;
        }

        // Agendamentos: os snapshots de contato.
        foreach (var ag in await db.Agendamentos.Where(a => a.ClienteId == clienteId).ToListAsync())
        {
            ag.NomeContato = marcador;
            ag.TelefoneContato = marcador;
            ag.EmailContato = null;
        }

        // Mensagens: destino e corpo têm PII; o evento (canal/tipo/status) fica
        // como trilha de auditoria sem o dado pessoal.
        foreach (var msg in await db.MensagensEnviadas.Where(m => m.ClienteId == clienteId).ToListAsync())
        {
            msg.Destino = marcador;
            msg.Corpo = "[conteúdo removido por anonimização LGPD]";
        }

        await db.SaveChangesAsync();
        return await clientes.ObterAsync(clienteId);
    }
}
