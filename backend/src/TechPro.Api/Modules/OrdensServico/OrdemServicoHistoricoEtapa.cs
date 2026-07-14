using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Trilha append-only de mudanças de etapa da OS: alimenta a linha do tempo
/// do portal e o SLA visual da Fase 2 (dados que não dá para reconstruir
/// depois). Também no escopo offline (status/histórico de Kanban).
/// </summary>
public class OrdemServicoHistoricoEtapa : ITenantEntity, IEntidadeSincronizavel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    /// <summary>Nulo na criação da OS (primeira entrada da trilha).</summary>
    public EtapaOrdemServico? DeEtapa { get; set; }

    public EtapaOrdemServico ParaEtapa { get; set; }

    /// <summary>Quem moveu (claim sub do JWT); nulo em movimentos do sistema.</summary>
    public Guid? UsuarioId { get; set; }

    public string? Motivo { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
