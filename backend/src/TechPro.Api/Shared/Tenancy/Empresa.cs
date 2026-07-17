namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// A raiz do tenant: cada assistência técnica cliente da TechPro é uma Empresa.
/// PK em UUID por decisão aprovada (não enumerável; viaja em JWT, URLs e RLS).
/// </summary>
public class Empresa
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }

    /// <summary>
    /// Identificador público da loja na URL de agendamento (/agendar/{slug}).
    /// Único globalmente; gerado do nome no cadastro e editável depois.
    /// </summary>
    public required string Slug { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>
    /// Marca quando o dono concluiu (ou pulou) o wizard de onboarding. Nulo =
    /// primeiro acesso → o front redireciona para /bem-vindo.
    /// </summary>
    public DateTimeOffset? OnboardingConcluidoEm { get; set; }

    // --- Dados da loja (módulo 13). Opcionais; exibidos ao cliente final nas
    // páginas públicas quando preenchidos. O logo depende do R2 (Fase 2).

    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }

    /// <summary>Políticas da loja (garantia, prazo de retirada...) em texto livre.</summary>
    public string? Politicas { get; set; }
}
