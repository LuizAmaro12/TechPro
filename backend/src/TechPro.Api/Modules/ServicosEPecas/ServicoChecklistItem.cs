using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Item do checklist padrão do serviço. Tabela própria (não jsonb): a Fase 2
/// marca item a item na OS ("checklist de qualidade por tipo de serviço").
/// </summary>
public class ServicoChecklistItem : ITenantEntity
{
    public int Id { get; set; }
    public int ServicoId { get; set; }
    public Guid TenantId { get; set; }
    public int Ordem { get; set; }
    public required string Descricao { get; set; }
}
