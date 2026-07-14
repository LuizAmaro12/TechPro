namespace TechPro.Api.Modules.OrdensServico.Dtos;

/// <summary>Criação manual (walk-in). Cliente e serviço são obrigatórios.</summary>
public record OrdemServicoRequest(
    int ClienteId,
    int ServicoId,
    int? AparelhoId,
    string? AparelhoMarca,
    string? AparelhoModelo,
    string? DescricaoProblema,
    PrioridadeOrdemServico Prioridade,
    DateOnly? PrazoEstimado,
    Guid? ResponsavelTecnicoId,
    string? Observacoes);

/// <summary>Edição de campos de gestão; cliente e serviço são fixos na Fase 1.</summary>
public record OrdemServicoAtualizacaoRequest(
    int? AparelhoId,
    string? AparelhoMarca,
    string? AparelhoModelo,
    string? DescricaoProblema,
    PrioridadeOrdemServico Prioridade,
    DateOnly? PrazoEstimado,
    Guid? ResponsavelTecnicoId,
    StatusPagamentoOrdemServico StatusPagamento,
    StatusAprovacaoOrdemServico StatusAprovacao,
    string? Observacoes);

public record MudancaEtapaRequest(EtapaOrdemServico ParaEtapa, string? Motivo);

public record OrdemServicoResponse(
    Guid Id,
    int Numero,
    EtapaOrdemServico Etapa,
    PrioridadeOrdemServico Prioridade,
    DateOnly? PrazoEstimado,
    int ClienteId,
    string ClienteNome,
    string ClienteTelefone,
    int ServicoId,
    string ServicoNome,
    int? AparelhoId,
    string? AparelhoMarca,
    string? AparelhoModelo,
    Guid? ResponsavelTecnicoId,
    string? ResponsavelTecnicoNome,
    StatusPagamentoOrdemServico StatusPagamento,
    StatusAprovacaoOrdemServico StatusAprovacao,
    string? DescricaoProblema,
    string? Observacoes,
    string? MotivoCancelamento,
    string CodigoAcompanhamento,
    int? AgendamentoId,
    DateTimeOffset CriadoEm,
    DateTimeOffset UpdatedAt);

public record HistoricoEtapaResponse(
    EtapaOrdemServico? DeEtapa,
    EtapaOrdemServico ParaEtapa,
    string? UsuarioNome,
    string? Motivo,
    DateTimeOffset CriadoEm);

public record OrdemServicoDetalheResponse(
    OrdemServicoResponse Ordem,
    List<HistoricoEtapaResponse> Historico);

// --- Sincronização por delta (contrato da Fase 2, seção 4 do doc de stack) ---

public record OrdemServicoSyncItem(
    OrdemServicoResponse Ordem,
    DateTimeOffset? DeletedAt);

public record HistoricoSyncItem(
    Guid Id,
    Guid OrdemServicoId,
    EtapaOrdemServico? DeEtapa,
    EtapaOrdemServico ParaEtapa,
    string? Motivo,
    DateTimeOffset CriadoEm,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public record OrdensServicoSyncResponse(
    List<OrdemServicoSyncItem> Ordens,
    List<HistoricoSyncItem> Historico,
    DateTimeOffset Agora);

// --- Equipe (responsável técnico) ---------------------------------------------

public record EquipeMembroResponse(Guid Id, string Nome, string Email);

// --- Acompanhamento público (portal do cliente, sem login) --------------------

public record AcompanhamentoResponse(
    string NomeLoja,
    int Numero,
    string ServicoNome,
    EtapaOrdemServico Etapa,
    DateOnly? PrazoEstimado,
    DateTimeOffset AtualizadoEm);
