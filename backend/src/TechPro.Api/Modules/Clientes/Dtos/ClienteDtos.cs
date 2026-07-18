namespace TechPro.Api.Modules.Clientes.Dtos;

public record ClienteRequest(
    string Nome,
    string Telefone,
    string? Email,
    string? Cpf,
    string? Endereco,
    string? Observacoes,
    bool Vip,
    bool ConsentiuComunicacoes,
    int? ClientePrincipalId,
    bool Ativo = true);

public record VinculoResponse(int Id, string Nome);

public record ClienteResponse(
    int Id,
    string Nome,
    string Telefone,
    string? Email,
    string? Cpf,
    string? Endereco,
    string? Observacoes,
    bool Vip,
    bool Ativo,
    VinculoResponse? ClientePrincipal,
    bool ConsentiuComunicacoes,
    DateTimeOffset? ConsentimentoEm,
    int QuantidadeAparelhos,
    DateTimeOffset? AnonimizadoEm);

public record ClienteDetalheResponse(
    int Id,
    string Nome,
    string Telefone,
    string? Email,
    string? Cpf,
    string? Endereco,
    string? Observacoes,
    bool Vip,
    bool Ativo,
    VinculoResponse? ClientePrincipal,
    bool ConsentiuComunicacoes,
    DateTimeOffset? ConsentimentoEm,
    DateTimeOffset? AnonimizadoEm,
    IReadOnlyList<AparelhoResponse> Aparelhos);

// --- Exportação LGPD (portabilidade) ------------------------------------------

public record DadosPessoaisResponse(
    ClienteDetalheResponse Cliente,
    IReadOnlyList<AgendamentoExportado> Agendamentos,
    IReadOnlyList<OrdemServicoExportada> OrdensServico,
    IReadOnlyList<MensagemExportada> Mensagens,
    DateTimeOffset GeradoEm);

public record AgendamentoExportado(
    int Id, string ServicoNome, DateOnly Data, TimeOnly HoraInicio, string Status,
    string NomeContato, string TelefoneContato, string? EmailContato);

public record OrdemServicoExportada(
    int Numero, string ServicoNome, string Etapa,
    string? AparelhoMarca, string? AparelhoModelo, DateTimeOffset CriadoEm);

public record MensagemExportada(
    string Canal, string TipoEvento, string Destino, string Status, DateTimeOffset CriadoEm);
