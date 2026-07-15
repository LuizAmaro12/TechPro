using System.Text.Json.Serialization;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Financeiro;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusOrcamento
{
    Rascunho,
    Enviado,
    Aprovado,
    Recusado,
}

/// <summary>
/// Orçamento da OS (um por OS na Fase 1, aprovação binária): mão de obra
/// editável + peças utilizadas (preço congelado no uso) − desconto. O envio
/// congela <see cref="ValorPecas"/> — o que o cliente vê não muda se a loja
/// registrar mais peças depois. Fora do escopo offline: aprovação exige
/// trilha append-only (seção 16), nunca last-write-wins.
/// </summary>
public class Orcamento : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    public decimal ValorMaoDeObra { get; set; }
    public decimal Desconto { get; set; }

    /// <summary>Congelado no envio; em rascunho a leitura calcula ao vivo.</summary>
    public decimal ValorPecas { get; set; }

    public StatusOrcamento Status { get; set; } = StatusOrcamento.Rascunho;
    public string? MotivoRecusa { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public DateTimeOffset? RespondidoEm { get; set; }

    public List<OrcamentoEvento> Eventos { get; set; } = [];
}
