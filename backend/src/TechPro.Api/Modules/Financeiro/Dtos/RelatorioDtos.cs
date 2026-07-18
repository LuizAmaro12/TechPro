namespace TechPro.Api.Modules.Financeiro.Dtos;

public record TransacaoResponse(
    int PagamentoId,
    Guid OrdemServicoId,
    int Numero,
    string ClienteNome,
    FormaPagamento Forma,
    decimal Valor,
    DateTimeOffset CriadoEm);

public record TotalPorFormaResponse(FormaPagamento Forma, decimal Total, int Quantidade);

/// <summary>OS com orçamento aprovado e saldo em aberto — receita vendida a receber.</summary>
public record PendenteResponse(
    Guid OrdemServicoId,
    int Numero,
    string ClienteNome,
    decimal Total,
    decimal Pago,
    decimal Saldo);

/// <summary>
/// "Quanto está para entrar" (item novo do doc): o que já foi aprovado e ainda
/// não foi pago + o valor esperado dos agendamentos dos próximos dias. O valor
/// dos agendamentos é estimativa pelo preço base do serviço.
/// </summary>
public record ProjecaoCaixaResponse(
    decimal AprovadosAReceber,
    decimal AgendamentosProximos7Dias,
    decimal Total);

// --- Rentabilidade (Fase 2): "quanto sobrou" -----------------------------------

public record RentabilidadePorServicoResponse(
    int ServicoId,
    string ServicoNome,
    int QuantidadeOs,
    decimal Receita,
    decimal CustoPecas,
    decimal LucroBruto,
    decimal MargemPercentual);

/// <summary>
/// Margem realizada das OS entregues no período (competência na entrega),
/// distinta do faturamento da Fase 1 que é caixa recebido.
/// </summary>
public record RentabilidadeResponse(
    DateOnly De,
    DateOnly Ate,
    int QuantidadeOs,
    int OsSemOrcamento,
    decimal ReceitaTotal,
    decimal CustoPecas,
    decimal LucroBruto,
    decimal MargemPercentual,
    List<RentabilidadePorServicoResponse> PorServico);

public record FinanceiroRelatorioResponse(
    DateOnly De,
    DateOnly Ate,
    decimal Faturamento,
    int QuantidadeOsPagas,
    decimal TicketMedio,
    int QuantidadeTransacoes,
    List<TransacaoResponse> Transacoes,
    List<TotalPorFormaResponse> PorForma,
    decimal AReceber,
    int QuantidadePendentes,
    List<PendenteResponse> Pendentes,
    ProjecaoCaixaResponse Projecao);
