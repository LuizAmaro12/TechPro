using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Peça utilizada em uma OS: registra a baixa automática no estoque com o
/// custo e o preço de venda congelados no momento do uso (margem real do
/// financeiro, módulo 11). Escopo offline — o app do técnico da Fase 2
/// registra peça usada em campo; remover é soft-delete (lápide) + devolução.
/// </summary>
public class OrdemServicoPeca : ITenantEntity, IEntidadeSincronizavel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    public int PecaId { get; set; }
    public Peca? Peca { get; set; }

    public int Quantidade { get; set; }

    /// <summary>Snapshot do catálogo no momento do uso — não muda depois.</summary>
    public decimal CustoUnitarioNoUso { get; set; }
    public decimal PrecoVendaNoUso { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
