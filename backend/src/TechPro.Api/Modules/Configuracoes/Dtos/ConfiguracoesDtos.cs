using TechPro.Api.Modules.Comunicacao;

namespace TechPro.Api.Modules.Configuracoes.Dtos;

// --- Dados da loja (módulo 13) ------------------------------------------------

public record LojaRequest(
    string Nome,
    string? Telefone,
    string? Email,
    string? Endereco,
    string? Politicas);

public record LojaResponse(
    string Nome,
    string Slug,
    string? Telefone,
    string? Email,
    string? Endereco,
    string? Politicas);

// --- Preferências de notificação (matriz evento × canal) ----------------------

public record PreferenciaItem(TipoEventoComunicacao TipoEvento, CanalNotificacao Canal, bool Ativo);

public record PreferenciasNotificacaoRequest(List<PreferenciaItem> Itens);

public record PreferenciasNotificacaoResponse(List<PreferenciaItem> Itens);

// --- Templates de mensagem por evento ------------------------------------------

/// <summary>
/// Texto efetivo do evento. <c>Personalizado = false</c> significa que a loja
/// ainda usa o padrão embutido — a tela mostra o padrão como ponto de partida.
/// </summary>
public record TemplateItem(
    TipoEventoComunicacao TipoEvento,
    string? Assunto,
    string Corpo,
    bool Personalizado,
    IReadOnlyList<string> VariaveisDisponiveis);

/// <summary>Corpo vazio remove a personalização (volta ao padrão).</summary>
public record TemplateSalvarItem(TipoEventoComunicacao TipoEvento, string? Assunto, string? Corpo);

public record TemplatesRequest(List<TemplateSalvarItem> Itens);

public record TemplatesResponse(List<TemplateItem> Itens);

// --- Conta do usuário ---------------------------------------------------------

public record ContaRequest(string Nome);

public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);
