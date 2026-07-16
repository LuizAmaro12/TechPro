using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class Servico : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public string? Categoria { get; set; }
    public decimal PrecoBase { get; set; }
    public int DuracaoEstimadaMinutos { get; set; }
    public int? PrazoMedioDias { get; set; }
    public bool ExigeDiagnostico { get; set; }
    public bool AgendavelOnline { get; set; }

    /// <summary>Atendimentos simultâneos que a agenda aceita para este serviço (módulo 2, "desde o início").</summary>
    public int CapacidadeSimultanea { get; set; } = 1;

    // Desativar em vez de apagar: o serviço pode estar referenciado por OS futuras.
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>Registro fictício de onboarding (dados de exemplo removíveis).</summary>
    public bool Exemplo { get; set; }
    public List<ServicoPeca> Pecas { get; set; } = [];
    public List<ServicoChecklistItem> Checklist { get; set; } = [];
}
