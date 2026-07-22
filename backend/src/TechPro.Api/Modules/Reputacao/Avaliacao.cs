using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Reputacao;

/// <summary>
/// Avaliação de um reparo entregue (módulo 10). Uma por OS — é o veredito
/// daquele serviço. Guarda estrela (experiência) e recomendação 0–10 (NPS), com
/// snapshot de serviço e técnico responsável na entrega. Avaliação negativa
/// abre um loop que o gestor fecha com uma nota de tratamento.
/// </summary>
public class Avaliacao : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }
    public OrdemServico? OrdemServico { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    /// <summary>Snapshot do serviço avaliado.</summary>
    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    /// <summary>Snapshot do responsável na entrega (a reatribuição tem trilha,
    /// mas a avaliação congela quem estava responsável).</summary>
    public Guid? ResponsavelTecnicoId { get; set; }

    /// <summary>Estrelas 1–5 (experiência do reparo).</summary>
    public int Nota { get; set; }

    /// <summary>Recomendação 0–10 (NPS).</summary>
    public int Recomendacao { get; set; }

    public string? Comentario { get; set; }

    // --- Fechamento de loop de avaliação negativa ---------------------------------

    public bool Resolvida { get; set; }
    public string? ResolucaoNota { get; set; }
    public DateTimeOffset? ResolvidaEm { get; set; }
    public Guid? ResolvidaPorUsuarioId { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>Sinal forte de insatisfação em qualquer das duas escalas.</summary>
    public bool EhNegativa => Nota <= 2 || Recomendacao <= 6;
}
