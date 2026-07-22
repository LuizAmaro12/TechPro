namespace TechPro.Api.Modules.Reputacao.Dtos;

/// <summary>Envio pelo cliente no acompanhamento público (sem login).</summary>
public record AvaliacaoRequest(int Nota, int Recomendacao, string? Comentario);

public record ResolverAvaliacaoRequest(string Nota);

public record AvaliacaoResponse(
    int Id,
    int OrdemServicoNumero,
    string? ClienteNome,
    string ServicoNome,
    string? TecnicoNome,
    int Nota,
    int Recomendacao,
    string? Comentario,
    bool Negativa,
    bool Resolvida,
    string? ResolucaoNota,
    DateTimeOffset? ResolvidaEm,
    DateTimeOffset CriadoEm);

// --- Resumo (média, distribuição, NPS, por técnico) ---------------------------

public record DistribuicaoEstrela(int Estrelas, int Quantidade);

public record NpsResponse(
    int Score,
    int Promotores,
    int Neutros,
    int Detratores);

public record SatisfacaoTecnicoResponse(
    Guid TecnicoId,
    string TecnicoNome,
    int Total,
    decimal MediaEstrelas,
    int Nps);

public record ResumoAvaliacoesResponse(
    int Total,
    decimal MediaEstrelas,
    List<DistribuicaoEstrela> Distribuicao,
    NpsResponse Nps,
    int PendenciasLoop,
    List<SatisfacaoTecnicoResponse> PorTecnico);
