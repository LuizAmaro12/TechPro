using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class Peca : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal PrecoVenda { get; set; }
    public int QuantidadeEmEstoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    // Desativar em vez de apagar: a peça pode estar referenciada por OS futuras.
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}
