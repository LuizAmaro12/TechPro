using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Dashboard.Dtos;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Modules.Dashboard;

/// <summary>
/// Agrega os indicadores da Fase 1 (módulo 12) a partir dos dados que já
/// existem — leitura pura sob GQF, sem entidade própria. "Hoje" e "mês" são a
/// hora de parede da loja (data UTC do servidor), como o resto da agenda.
/// </summary>
public class DashboardService(TechProDbContext db)
{
    private const int MaxRadar = 10;
    private const int DiasParaOrcamentoPendente = 2;

    /// <summary>Etapas de bancada — "aparelhos em reparo" (da fila ao teste).</summary>
    private static readonly EtapaOrdemServico[] EtapasDeBancada =
    [
        EtapaOrdemServico.NaFila,
        EtapaOrdemServico.EmDiagnostico,
        EtapaOrdemServico.AguardandoAprovacao,
        EtapaOrdemServico.AguardandoPeca,
        EtapaOrdemServico.EmReparo,
        EtapaOrdemServico.EmTeste,
    ];

    private static readonly EtapaOrdemServico[] EtapasFinalizadas =
    [
        EtapaOrdemServico.Entregue,
        EtapaOrdemServico.Cancelado,
    ];

    public async Task<DashboardResponse> ObterAsync()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioMes = new DateTimeOffset(hoje.Year, hoje.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var inicioProximoMes = inicioMes.AddMonths(1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);

        var osAtivas = db.OrdensServico.Where(o => o.DeletedAt == null);

        var osAbertas = await osAtivas
            .CountAsync(o => !EtapasFinalizadas.Contains(o.Etapa));
        var aparelhosEmReparo = await osAtivas
            .CountAsync(o => EtapasDeBancada.Contains(o.Etapa));
        var prontosParaRetirada = await osAtivas
            .CountAsync(o => o.Etapa == EtapaOrdemServico.ProntoParaRetirada);
        var servicosEmAtraso = await osAtivas
            .CountAsync(o => !EtapasFinalizadas.Contains(o.Etapa)
                && o.PrazoEstimado != null && o.PrazoEstimado < hoje);

        var agendamentosHoje = await db.Agendamentos
            .CountAsync(a => a.Data == hoje && a.Status == StatusAgendamento.Agendado);

        var faturamentoMes = await SomarPagamentosAsync(inicioMes, inicioProximoMes);
        var faturamentoMesAnterior = await SomarPagamentosAsync(inicioMesAnterior, inicioMes);
        var variacao = faturamentoMesAnterior > 0
            ? Math.Round((faturamentoMes - faturamentoMesAnterior) / faturamentoMesAnterior * 100, 1)
            : (decimal?)null;

        var radar = await MontarRadarAsync(hoje);

        return new DashboardResponse(
            osAbertas,
            agendamentosHoje,
            servicosEmAtraso,
            aparelhosEmReparo,
            prontosParaRetirada,
            faturamentoMes,
            faturamentoMesAnterior,
            variacao,
            radar);
    }

    private async Task<decimal> SomarPagamentosAsync(DateTimeOffset de, DateTimeOffset ate) =>
        // Soma em memória: o Sqlite dos testes não agrega decimal no servidor.
        (await db.Pagamentos
            .Where(p => p.CriadoEm >= de && p.CriadoEm < ate)
            .Select(p => p.Valor)
            .ToListAsync())
        .Sum();

    private async Task<RadarResponse> MontarRadarAsync(DateOnly hoje)
    {
        var atrasadasQuery = db.OrdensServico
            .Where(o => o.DeletedAt == null
                && !EtapasFinalizadas.Contains(o.Etapa)
                && o.PrazoEstimado != null && o.PrazoEstimado < hoje);

        var totalAtrasadas = await atrasadasQuery.CountAsync();
        var atrasadas = await atrasadasQuery
            .OrderBy(o => o.PrazoEstimado) // mais atrasada primeiro
            .Take(MaxRadar)
            .Select(o => new
            {
                o.Id,
                o.Numero,
                ClienteNome = o.Cliente!.Nome,
                ServicoNome = o.Servico!.Nome,
                o.PrazoEstimado,
            })
            .ToListAsync();

        var corte = DateTimeOffset.UtcNow.AddDays(-DiasParaOrcamentoPendente);
        var pendentesQuery =
            from orcamento in db.Orcamentos
            join os in db.OrdensServico on orcamento.OrdemServicoId equals os.Id
            where orcamento.Status == StatusOrcamento.Enviado
                && orcamento.EnviadoEm != null && orcamento.EnviadoEm < corte
                && os.DeletedAt == null
            orderby orcamento.EnviadoEm // aguardando há mais tempo primeiro
            select new
            {
                os.Id, // a OS é o destino do link no radar
                os.Numero,
                ClienteNome = os.Cliente!.Nome,
                Total = orcamento.ValorMaoDeObra + orcamento.ValorPecas - orcamento.Desconto,
                orcamento.EnviadoEm,
            };

        var pendentesLista = await pendentesQuery.ToListAsync();
        var agora = DateTimeOffset.UtcNow;

        return new RadarResponse(
            atrasadas.Select(o => new OsAtrasadaResponse(
                o.Id, o.Numero, o.ClienteNome, o.ServicoNome,
                o.PrazoEstimado!.Value, hoje.DayNumber - o.PrazoEstimado.Value.DayNumber)).ToList(),
            totalAtrasadas,
            pendentesLista.Take(MaxRadar).Select(p => new OrcamentoPendenteResponse(
                p.Id, p.Numero, p.ClienteNome, p.Total,
                p.EnviadoEm!.Value, (agora - p.EnviadoEm.Value).Days)).ToList(),
            pendentesLista.Count);
    }
}
