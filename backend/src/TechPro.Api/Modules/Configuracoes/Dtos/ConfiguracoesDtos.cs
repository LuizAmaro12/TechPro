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

// --- Conta do usuário ---------------------------------------------------------

public record ContaRequest(string Nome);

public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);
