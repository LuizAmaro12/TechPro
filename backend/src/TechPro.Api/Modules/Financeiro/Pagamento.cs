using System.Text.Json.Serialization;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Financeiro;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FormaPagamento
{
    Dinheiro,
    Pix,
    CartaoDebito,
    CartaoCredito,
    Outro,
}

/// <summary>
/// Pagamento registrado na OS (sem gateway na Fase 1): vários por OS —
/// entrada + saldo na retirada é o dia a dia do setor. O status de pagamento
/// da OS é derivado da soma vs. total do orçamento. Remoção física permitida
/// (erro de digitação); estorno formal fica para o financeiro da Fase 2/3.
/// </summary>
public class Pagamento : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    public decimal Valor { get; set; }
    public FormaPagamento Forma { get; set; }
    public string? Observacao { get; set; }

    public Guid? RegistradoPorUsuarioId { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
