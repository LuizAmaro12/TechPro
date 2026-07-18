using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Financeiro.Dtos;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Modules.Financeiro;

/// <summary>
/// Visão de caixa do módulo 8 (Fase 1): faturamento por período, transações,
/// a receber, ticket médio e projeção. Leitura pura sob GQF — o dado já está em
/// pagamentos/orçamentos/OS/agendamentos. Margem e rentabilidade são Fase 2;
/// a base para elas (custo x preço congelados na peça da OS) já existe.
/// </summary>
public class FinanceiroRelatorioService(TechProDbContext db)
{
    private const int MaxTransacoes = 100;
    private const int MaxPendentes = 50;
    private const int DiasDeProjecao = 7;

    public async Task<FinanceiroRelatorioResponse> ObterAsync(DateOnly de, DateOnly ate)
    {
        // Período é hora de parede da loja; o filtro cobre o dia inteiro de "ate".
        var inicio = new DateTimeOffset(de.Year, de.Month, de.Day, 0, 0, 0, TimeSpan.Zero);
        var fim = new DateTimeOffset(ate.Year, ate.Month, ate.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1);

        var transacoes = await CarregarTransacoesAsync(inicio, fim);
        var faturamento = transacoes.Sum(t => t.Valor);
        var osPagas = transacoes.Select(t => t.OrdemServicoId).Distinct().Count();
        var ticketMedio = osPagas > 0 ? Math.Round(faturamento / osPagas, 2) : 0m;

        var porForma = transacoes
            .GroupBy(t => t.Forma)
            .Select(g => new TotalPorFormaResponse(g.Key, g.Sum(t => t.Valor), g.Count()))
            .OrderByDescending(f => f.Total)
            .ToList();

        var pendentes = await CarregarPendentesAsync();
        var aReceber = pendentes.Sum(p => p.Saldo);
        var esperadoAgendamentos = await EsperadoDosAgendamentosAsync();

        return new FinanceiroRelatorioResponse(
            de,
            ate,
            faturamento,
            osPagas,
            ticketMedio,
            transacoes.Count,
            transacoes.Take(MaxTransacoes).ToList(),
            porForma,
            aReceber,
            pendentes.Count,
            pendentes.Take(MaxPendentes).ToList(),
            new ProjecaoCaixaResponse(
                aReceber,
                esperadoAgendamentos,
                aReceber + esperadoAgendamentos));
    }

    /// <summary>
    /// Margem realizada: OS **entregues** no período (decisão 2026-07-18).
    /// Receita = total do orçamento; custo = peças com o custo congelado no uso
    /// — por isso a margem é histórica e não muda se o catálogo mudar depois.
    /// </summary>
    public async Task<RentabilidadeResponse> ObterRentabilidadeAsync(DateOnly de, DateOnly ate)
    {
        var inicio = new DateTimeOffset(de.Year, de.Month, de.Day, 0, 0, 0, TimeSpan.Zero);
        var fim = new DateTimeOffset(ate.Year, ate.Month, ate.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1);

        // 1ª transição para Entregue de cada OS (agrupamento em memória — o
        // Sqlite dos testes não agrega DateTimeOffset no servidor).
        var entregasNoPeriodo = (await db.HistoricosEtapaOrdemServico
                .Where(h => h.ParaEtapa == EtapaOrdemServico.Entregue && h.DeletedAt == null)
                .Select(h => new { h.OrdemServicoId, h.CriadoEm })
                .ToListAsync())
            .GroupBy(h => h.OrdemServicoId)
            .Select(g => new { OrdemServicoId = g.Key, EntregueEm = g.Min(h => h.CriadoEm) })
            .Where(e => e.EntregueEm >= inicio && e.EntregueEm < fim)
            .Select(e => e.OrdemServicoId)
            .ToList();

        if (entregasNoPeriodo.Count == 0)
        {
            return new RentabilidadeResponse(de, ate, 0, 0, 0m, 0m, 0m, 0m, []);
        }

        // Só conta se a OS ainda está Entregue (se voltou atrás, não conta).
        var ordens = await db.OrdensServico
            .Where(o => entregasNoPeriodo.Contains(o.Id)
                && o.DeletedAt == null
                && o.Etapa == EtapaOrdemServico.Entregue)
            .Select(o => new { o.Id, o.ServicoId, ServicoNome = o.Servico!.Nome })
            .ToListAsync();

        var ids = ordens.Select(o => o.Id).ToList();

        var receitaPorOs = (await db.Orcamentos
                .Where(o => ids.Contains(o.OrdemServicoId))
                .Select(o => new
                {
                    o.OrdemServicoId,
                    Total = o.ValorMaoDeObra + o.ValorPecas - o.Desconto,
                })
                .ToListAsync())
            .ToDictionary(o => o.OrdemServicoId, o => o.Total);

        var custoPorOs = (await db.OrdensServicoPecas
                .Where(p => ids.Contains(p.OrdemServicoId) && p.DeletedAt == null)
                .Select(p => new { p.OrdemServicoId, p.CustoUnitarioNoUso, p.Quantidade })
                .ToListAsync())
            .GroupBy(p => p.OrdemServicoId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.CustoUnitarioNoUso * p.Quantidade));

        var porServico = ordens
            .GroupBy(o => new { o.ServicoId, o.ServicoNome })
            .Select(g =>
            {
                var receita = g.Sum(o => receitaPorOs.GetValueOrDefault(o.Id, 0m));
                var custo = g.Sum(o => custoPorOs.GetValueOrDefault(o.Id, 0m));
                return new RentabilidadePorServicoResponse(
                    g.Key.ServicoId, g.Key.ServicoNome, g.Count(),
                    receita, custo, receita - custo, Margem(receita - custo, receita));
            })
            .OrderByDescending(s => s.LucroBruto)
            .ToList();

        var receitaTotal = porServico.Sum(s => s.Receita);
        var custoTotal = porServico.Sum(s => s.CustoPecas);

        return new RentabilidadeResponse(
            de,
            ate,
            ordens.Count,
            ordens.Count(o => !receitaPorOs.ContainsKey(o.Id)),
            receitaTotal,
            custoTotal,
            receitaTotal - custoTotal,
            Margem(receitaTotal - custoTotal, receitaTotal),
            porServico);
    }

    private static decimal Margem(decimal lucro, decimal receita) =>
        receita > 0 ? Math.Round(lucro / receita * 100, 1) : 0m;

    private async Task<List<TransacaoResponse>> CarregarTransacoesAsync(
        DateTimeOffset inicio, DateTimeOffset fim)
    {
        var linhas = await (
            from pagamento in db.Pagamentos
            join os in db.OrdensServico on pagamento.OrdemServicoId equals os.Id
            where pagamento.CriadoEm >= inicio && pagamento.CriadoEm < fim
            select new TransacaoResponse(
                pagamento.Id,
                os.Id,
                os.Numero,
                os.Cliente!.Nome,
                pagamento.Forma,
                pagamento.Valor,
                pagamento.CriadoEm)).ToListAsync();

        // Ordenação em memória: Sqlite (testes) não ordena DateTimeOffset.
        return linhas.OrderByDescending(t => t.CriadoEm).ToList();
    }

    /// <summary>
    /// A receber = OS viva com orçamento APROVADO e saldo em aberto (decisão
    /// 2026-07-16). Orçamento só enviado é proposta, não receita vendida.
    /// </summary>
    private async Task<List<PendenteResponse>> CarregarPendentesAsync()
    {
        var aprovados = await (
            from orcamento in db.Orcamentos
            join os in db.OrdensServico on orcamento.OrdemServicoId equals os.Id
            where orcamento.Status == StatusOrcamento.Aprovado
                && os.DeletedAt == null
                && os.Etapa != EtapaOrdemServico.Cancelado
            select new
            {
                OrdemServicoId = os.Id,
                os.Numero,
                ClienteNome = os.Cliente!.Nome,
                Total = orcamento.ValorMaoDeObra + orcamento.ValorPecas - orcamento.Desconto,
            }).ToListAsync();

        if (aprovados.Count == 0)
        {
            return [];
        }

        var ids = aprovados.Select(a => a.OrdemServicoId).ToList();
        var pagoPorOs = (await db.Pagamentos
                .Where(p => ids.Contains(p.OrdemServicoId))
                .Select(p => new { p.OrdemServicoId, p.Valor })
                .ToListAsync())
            .GroupBy(p => p.OrdemServicoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Valor));

        return aprovados
            .Select(a =>
            {
                var pago = pagoPorOs.GetValueOrDefault(a.OrdemServicoId, 0m);
                return new PendenteResponse(
                    a.OrdemServicoId, a.Numero, a.ClienteNome, a.Total, pago, a.Total - pago);
            })
            .Where(p => p.Saldo > 0)
            .OrderByDescending(p => p.Saldo)
            .ToList();
    }

    /// <summary>
    /// Estimativa do que os agendamentos dos próximos dias devem gerar —
    /// preço base do serviço (o orçamento real pode diferir; a UI avisa).
    /// </summary>
    private async Task<decimal> EsperadoDosAgendamentosAsync()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var limite = hoje.AddDays(DiasDeProjecao);
        return (await db.Agendamentos
                .Where(a => a.Status == StatusAgendamento.Agendado
                    && a.Data >= hoje && a.Data <= limite)
                .Select(a => a.Servico!.PrecoBase)
                .ToListAsync())
            .Sum();
    }
}
