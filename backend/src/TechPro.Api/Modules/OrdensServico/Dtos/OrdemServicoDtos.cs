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

/// <summary>
/// Edição de campos de gestão; cliente e serviço são fixos na Fase 1.
/// Status de pagamento e de aprovação saíram daqui — desde a etapa de
/// orçamento/pagamento (2026-07-15) são derivados dos fluxos reais.
/// </summary>
public record OrdemServicoAtualizacaoRequest(
    int? AparelhoId,
    string? AparelhoMarca,
    string? AparelhoModelo,
    string? DescricaoProblema,
    PrioridadeOrdemServico Prioridade,
    DateOnly? PrazoEstimado,
    Guid? ResponsavelTecnicoId,
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
    DateTimeOffset UpdatedAt,
    /// <summary>Horas na etapa atual — o servidor calcula, o Kanban só compara.</summary>
    decimal HorasNaEtapa,
    /// <summary>Limite do serviço; nulo em etapa final (OS parada de propósito não é atraso).</summary>
    int? SlaHoras);

public record HistoricoEtapaResponse(
    EtapaOrdemServico? DeEtapa,
    EtapaOrdemServico ParaEtapa,
    string? UsuarioNome,
    string? Motivo,
    DateTimeOffset CriadoEm);

public record OrdemServicoDetalheResponse(
    OrdemServicoResponse Ordem,
    List<HistoricoEtapaResponse> Historico,
    List<PecaUsadaResponse> Pecas,
    Financeiro.Dtos.OrcamentoResponse? Orcamento,
    Financeiro.Dtos.ResumoPagamentosResponse? Pagamentos,
    List<ComentarioResponse> Comentarios,
    List<ReatribuicaoResponse> Reatribuicoes);

// --- Comentários internos e reatribuição (Fase 2) -----------------------------

public record ComentarioRequest(string Texto);

/// <summary>Nunca sai no portal do cliente — é registro interno da loja.</summary>
public record ComentarioResponse(
    Guid Id,
    string Texto,
    Guid? AutorUsuarioId,
    string? AutorNome,
    DateTimeOffset CriadoEm);

/// <summary>Motivo é obrigatório: o valor do recurso é a rastreabilidade.</summary>
public record ReatribuicaoRequest(Guid? ResponsavelTecnicoId, string Motivo);

public record ReatribuicaoResponse(
    int Id,
    Guid? DeUsuarioId,
    string? DeNome,
    Guid? ParaUsuarioId,
    string? ParaNome,
    string Motivo,
    string? PorNome,
    DateTimeOffset CriadoEm);

// --- Peças utilizadas (baixa automática, módulo 7) -----------------------------

public record PecaUsadaRequest(int PecaId, int Quantidade);

/// <summary>Flags de estoque pós-baixa: a UI avisa, nunca bloqueia (decisão 2026-07-15).</summary>
public record PecaUsadaResponse(
    Guid Id,
    int PecaId,
    string PecaNome,
    int Quantidade,
    decimal CustoUnitarioNoUso,
    decimal PrecoVendaNoUso,
    int EstoqueRestante,
    bool EstoqueAbaixoDoMinimo,
    bool EstoqueNegativo,
    DateTimeOffset CriadoEm);

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

public record PecaUsadaSyncItem(
    Guid Id,
    Guid OrdemServicoId,
    int PecaId,
    int Quantidade,
    decimal CustoUnitarioNoUso,
    decimal PrecoVendaNoUso,
    DateTimeOffset CriadoEm,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

/// <summary>Comentário no delta: o técnico comenta em campo, então entra no
/// escopo offline desde o primeiro migration (regra do doc de stack).</summary>
public record ComentarioSyncItem(
    Guid Id,
    Guid OrdemServicoId,
    string Texto,
    Guid? AutorUsuarioId,
    DateTimeOffset CriadoEm,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public record OrdensServicoSyncResponse(
    List<OrdemServicoSyncItem> Ordens,
    List<HistoricoSyncItem> Historico,
    List<PecaUsadaSyncItem> PecasUtilizadas,
    List<ComentarioSyncItem> Comentarios,
    DateTimeOffset Agora);

// --- Equipe (responsável técnico) ---------------------------------------------

public record EquipeMembroResponse(Guid Id, string Nome, string Email);

// --- Acompanhamento público (portal do cliente, sem login) --------------------

/// <summary>Etapa percorrida pela OS e quando foi alcançada — projeção
/// client-safe do histórico (sem usuário nem motivo internos).</summary>
public record EtapaAlcancadaResponse(EtapaOrdemServico Etapa, DateTimeOffset AlcancadaEm);

public record AcompanhamentoResponse(
    string NomeLoja,
    int Numero,
    string ServicoNome,
    EtapaOrdemServico Etapa,
    DateOnly? PrazoEstimado,
    DateTimeOffset AtualizadoEm,
    Financeiro.Dtos.OrcamentoPublicoResponse? Orcamento,
    Agendamentos.Dtos.LojaContatoResponse Contato,
    List<EtapaAlcancadaResponse> LinhaDoTempo,
    /// <summary>OS entregue e ainda sem avaliação — o portal mostra o formulário.</summary>
    bool PodeAvaliar,
    bool JaAvaliada);
