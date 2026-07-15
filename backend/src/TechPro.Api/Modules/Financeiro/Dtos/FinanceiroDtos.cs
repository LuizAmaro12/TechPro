using TechPro.Api.Modules.OrdensServico;

namespace TechPro.Api.Modules.Financeiro.Dtos;

// --- Orçamento -----------------------------------------------------------------

public record OrcamentoRequest(decimal ValorMaoDeObra, decimal Desconto);

/// <summary>Aprovar/recusar — o motivo é opcional na aprovação, livre na recusa.</summary>
public record RespostaOrcamentoRequest(string? Motivo);

public record OrcamentoEventoResponse(
    TipoEventoOrcamento Tipo,
    CanalEventoOrcamento Canal,
    string? UsuarioNome,
    decimal ValorTotal,
    string? Motivo,
    DateTimeOffset CriadoEm);

public record OrcamentoResponse(
    int Id,
    StatusOrcamento Status,
    decimal ValorMaoDeObra,
    decimal ValorPecas,
    decimal Desconto,
    decimal Total,
    string? MotivoRecusa,
    DateTimeOffset? EnviadoEm,
    DateTimeOffset? RespondidoEm,
    List<OrcamentoEventoResponse> Eventos);

// --- Pagamentos -----------------------------------------------------------------

public record PagamentoRequest(decimal Valor, FormaPagamento Forma, string? Observacao);

public record PagamentoResponse(
    int Id,
    decimal Valor,
    FormaPagamento Forma,
    string? Observacao,
    string? RegistradoPorNome,
    DateTimeOffset CriadoEm);

public record ResumoPagamentosResponse(
    List<PagamentoResponse> Pagamentos,
    decimal TotalPago,
    decimal? TotalOrcamento,
    decimal? Saldo,
    StatusPagamentoOrdemServico Status);

// --- Portal público ---------------------------------------------------------------

/// <summary>Só valores e status — nada além do que o cliente precisa decidir.</summary>
public record OrcamentoPublicoResponse(
    decimal ValorMaoDeObra,
    decimal ValorPecas,
    decimal Desconto,
    decimal Total,
    StatusOrcamento Status,
    DateTimeOffset? EnviadoEm,
    DateTimeOffset? RespondidoEm);
