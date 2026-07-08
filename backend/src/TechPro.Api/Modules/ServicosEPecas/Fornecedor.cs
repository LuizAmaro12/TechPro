using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Fornecedor de peças. Entidade própria (não campo texto) porque a Fase 2
/// exige histórico de preço de compra por fornecedor — normalizar strings
/// digitadas à mão, com dados reais, custaria muito mais depois.
/// </summary>
public class Fornecedor : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public string? Contato { get; set; }
}
