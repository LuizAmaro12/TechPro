using System.Text.Json.Serialization;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoMovimentacaoEstoque
{
    /// <summary>Compra recebida / reposição.</summary>
    Entrada,

    /// <summary>Saída manual (perda, devolução ao fornecedor, uso interno).</summary>
    Saida,

    /// <summary>Correção de contagem — exige motivo.</summary>
    Ajuste,

    /// <summary>Baixa automática por peça usada na OS.</summary>
    ConsumoOs,

    /// <summary>Devolução ao estoque quando a peça sai da OS.</summary>
    EstornoOs,
}

/// <summary>
/// Razão append-only do estoque. <see cref="Quantidade"/> é **assinada**
/// (entrada positiva, saída negativa), de modo que a soma reconcilia com o
/// saldo da peça — auditoria vira consulta, não algoritmo.
///
/// Fora do escopo offline de propósito: é registro administrativo derivado. O
/// que o técnico gera em campo é a peça usada na OS, que já sincroniza; o
/// movimento de consumo é consequência dela, não uma segunda entrada.
/// </summary>
public class MovimentacaoEstoque : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public int PecaId { get; set; }
    public Peca? Peca { get; set; }

    public TipoMovimentacaoEstoque Tipo { get; set; }

    /// <summary>Delta assinado aplicado ao saldo.</summary>
    public int Quantidade { get; set; }

    /// <summary>Saldo resultante — deixa o extrato legível sem soma corrida e
    /// transforma qualquer divergência futura em evidência.</summary>
    public int SaldoApos { get; set; }

    /// <summary>Custo pago na entrada; base do histórico de preço por fornecedor.</summary>
    public decimal? CustoUnitario { get; set; }

    public string? Motivo { get; set; }

    /// <summary>Preenchido nos movimentos originados de uma OS.</summary>
    public Guid? OrdemServicoId { get; set; }

    public Guid? UsuarioId { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}
