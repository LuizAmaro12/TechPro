namespace TechPro.Api.Modules.Agendamentos.Dtos;

// DTOs da rota pública (sem login): expõem apenas o que a página de
// agendamento precisa — nunca dados de clientes já cadastrados.

public record ServicoPublicoResponse(
    int Id,
    string Nome,
    string? Categoria,
    decimal PrecoBase,
    int DuracaoEstimadaMinutos,
    int? PrazoMedioDias,
    bool ExigeDiagnostico);

/// <summary>Contato e políticas da loja exibidos ao cliente final (módulo 13).</summary>
public record LojaContatoResponse(
    string? Telefone,
    string? Email,
    string? Endereco,
    string? Politicas);

public record LojaPublicaResponse(
    string Nome,
    string Slug,
    LojaContatoResponse Contato,
    List<ServicoPublicoResponse> Servicos);

public record AgendamentoPublicoRequest(
    int ServicoId,
    DateOnly Data,
    TimeOnly HoraInicio,
    string NomeContato,
    string TelefoneContato,
    string? EmailContato,
    string? DescricaoProblema,
    string AparelhoMarca,
    string AparelhoModelo);

/// <summary>Confirmação pública: só o resumo do que a própria pessoa enviou.</summary>
public record AgendamentoPublicoResponse(
    int Id,
    string NomeLoja,
    string ServicoNome,
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim);
