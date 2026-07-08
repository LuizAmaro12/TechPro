using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>"Peças normalmente utilizadas" pelo serviço (módulo 6), com quantidade padrão.</summary>
public class ServicoPeca : ITenantEntity
{
    public int ServicoId { get; set; }
    public int PecaId { get; set; }
    public Guid TenantId { get; set; }
    public int QuantidadePadrao { get; set; } = 1;
    public Peca? Peca { get; set; }
}
