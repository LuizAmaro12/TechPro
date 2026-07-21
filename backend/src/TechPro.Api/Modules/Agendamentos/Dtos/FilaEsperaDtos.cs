namespace TechPro.Api.Modules.Agendamentos.Dtos;

/// <summary>Entrada na fila pelo portal público (sem login).</summary>
public record FilaEsperaPublicaRequest(
    int ServicoId,
    string NomeContato,
    string TelefoneContato,
    string? EmailContato,
    DateOnly? DataPreferida,
    string? DescricaoProblema,
    string? AparelhoMarca,
    string? AparelhoModelo);

/// <summary>Entrada manual pela loja (quem ligou/apareceu). Com ClienteId, o
/// contato pode vir do cadastro.</summary>
public record FilaEsperaRequest(
    int ServicoId,
    int? ClienteId,
    string? NomeContato,
    string? TelefoneContato,
    string? EmailContato,
    DateOnly? DataPreferida,
    string? DescricaoProblema,
    string? AparelhoMarca,
    string? AparelhoModelo);

/// <summary>Converte a entrada em agendamento: a loja escolhe a vaga.</summary>
public record ConverterFilaRequest(DateOnly Data, TimeOnly HoraInicio);

public record DescartarFilaRequest(string? Motivo);

public record FilaEsperaResponse(
    int Id,
    int ServicoId,
    string ServicoNome,
    int? ClienteId,
    string NomeContato,
    string TelefoneContato,
    string? EmailContato,
    DateOnly? DataPreferida,
    string? DescricaoProblema,
    string? AparelhoMarca,
    string? AparelhoModelo,
    OrigemAgendamento Origem,
    StatusFilaEspera Status,
    int? AgendamentoId,
    DateTimeOffset CriadoEm);

/// <summary>Confirmação pública: só o que a própria pessoa enviou.</summary>
public record FilaEsperaPublicaResponse(int Id, string NomeLoja, string ServicoNome);
