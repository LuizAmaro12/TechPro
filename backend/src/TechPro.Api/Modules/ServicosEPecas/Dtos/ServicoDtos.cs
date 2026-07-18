namespace TechPro.Api.Modules.ServicosEPecas.Dtos;

public record ServicoPecaRequest(int PecaId, int QuantidadePadrao);

public record ServicoRequest(
    string Nome,
    string? Categoria,
    decimal PrecoBase,
    int DuracaoEstimadaMinutos,
    int? PrazoMedioDias,
    bool ExigeDiagnostico,
    bool AgendavelOnline,
    int CapacidadeSimultanea,
    /// <summary>Horas por etapa antes do card do Kanban alertar; nulo = sem SLA.</summary>
    int? SlaHoras,
    bool Ativo,
    IReadOnlyList<string> Checklist,
    IReadOnlyList<ServicoPecaRequest> Pecas);

public record ServicoPecaResponse(int PecaId, string Nome, int QuantidadePadrao);

public record ServicoResponse(
    int Id,
    string Nome,
    string? Categoria,
    decimal PrecoBase,
    int DuracaoEstimadaMinutos,
    int? PrazoMedioDias,
    bool ExigeDiagnostico,
    bool AgendavelOnline,
    int CapacidadeSimultanea,
    /// <summary>Horas por etapa antes do card do Kanban alertar; nulo = sem SLA.</summary>
    int? SlaHoras,
    bool Ativo,
    IReadOnlyList<string> Checklist,
    IReadOnlyList<ServicoPecaResponse> Pecas);
