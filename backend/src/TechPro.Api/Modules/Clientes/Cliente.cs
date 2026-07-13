using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Clientes;

/// <summary>
/// Cliente final da assistência (módulo 5 — CRM). Exclusão é desativação;
/// o direito de exclusão LGPD vira anonimização na Fase 2 (seção 16 do doc
/// de stack), trocando os campos pessoais por marcadores sem quebrar FKs.
/// </summary>
public class Cliente : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public required string Telefone { get; set; }
    public string? Email { get; set; }
    public string? Cpf { get; set; }
    public string? Endereco { get; set; }
    public string? Observacoes { get; set; }

    /// <summary>Marcado manualmente pela loja; "recorrente" será derivado das OS.</summary>
    public bool Vip { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Conta vinculada família/empresa (módulo 5) — 1 nível: quem tem
    /// principal não pode ter vinculados, e vice-versa.
    /// </summary>
    public int? ClientePrincipalId { get; set; }
    public Cliente? ClientePrincipal { get; set; }

    /// <summary>Base mínima de consentimento para comunicações operacionais (módulo 14, Fase 1).</summary>
    public bool ConsentiuComunicacoes { get; set; }
    public DateTimeOffset? ConsentimentoEm { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public List<Aparelho> Aparelhos { get; set; } = [];
}
