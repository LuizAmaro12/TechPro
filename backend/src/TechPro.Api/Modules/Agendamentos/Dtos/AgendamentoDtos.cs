namespace TechPro.Api.Modules.Agendamentos.Dtos;

/// <summary>
/// Criação/edição manual pelo time da loja. Com ClienteId, o snapshot de
/// contato pode ser omitido (vem do cadastro do cliente); sem ClienteId,
/// nome e telefone de contato são obrigatórios.
/// </summary>
public record AgendamentoRequest(
    int ServicoId,
    DateOnly Data,
    TimeOnly HoraInicio,
    int? ClienteId,
    string? NomeContato,
    string? TelefoneContato,
    string? EmailContato,
    string? DescricaoProblema,
    string? AparelhoMarca,
    string? AparelhoModelo);

public record CancelamentoRequest(string? Motivo);

public record AgendamentoResponse(
    int Id,
    StatusAgendamento Status,
    OrigemAgendamento Origem,
    int ServicoId,
    string ServicoNome,
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    int? ClienteId,
    string NomeContato,
    string TelefoneContato,
    string? EmailContato,
    string? DescricaoProblema,
    string? AparelhoMarca,
    string? AparelhoModelo,
    DateTimeOffset CriadoEm,
    DateTimeOffset? ReagendadoEm,
    DateTimeOffset? CanceladoEm,
    string? MotivoCancelamento);
