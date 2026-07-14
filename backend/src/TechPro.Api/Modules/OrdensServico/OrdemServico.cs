using System.Text.Json.Serialization;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Auth;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Etapas do fluxo da OS (módulo 3). A coluna "Agendado" do Kanban mostra
/// agendamentos que ainda não viraram OS — por isso a OS em si nunca está
/// "Agendado" e o enum começa no check-in.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EtapaOrdemServico
{
    CheckInRealizado,
    NaFila,
    EmDiagnostico,
    AguardandoAprovacao,
    AguardandoPeca,
    EmReparo,
    EmTeste,
    ProntoParaRetirada,
    Entregue,
    Cancelado,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrioridadeOrdemServico
{
    Baixa,
    Normal,
    Alta,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusPagamentoOrdemServico
{
    NaoPago,
    Parcial,
    Pago,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusAprovacaoOrdemServico
{
    Pendente,
    Aprovado,
    Recusado,
}

/// <summary>
/// O registro único e visual de tudo que acontece com o aparelho, do
/// check-in à entrega. Escopo offline do técnico: UUID + colunas de sync.
/// Um serviço principal por OS na Fase 1 (orçamento item a item é Fase 2).
/// </summary>
public class OrdemServico : ITenantEntity, IEntidadeSincronizavel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>Número amigável, sequencial e único por empresa ("OS #124").</summary>
    public int Numero { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? AparelhoId { get; set; }
    public Aparelho? Aparelho { get; set; }
    public string? AparelhoMarca { get; set; }
    public string? AparelhoModelo { get; set; }

    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public int? AgendamentoId { get; set; }
    public Agendamento? Agendamento { get; set; }

    public EtapaOrdemServico Etapa { get; set; } = EtapaOrdemServico.CheckInRealizado;
    public PrioridadeOrdemServico Prioridade { get; set; } = PrioridadeOrdemServico.Normal;
    public DateOnly? PrazoEstimado { get; set; }

    public Guid? ResponsavelTecnicoId { get; set; }
    public Usuario? ResponsavelTecnico { get; set; }

    // Campos manuais até os módulos de estoque/orçamento/pagamento (etapas 6-7).
    public StatusPagamentoOrdemServico StatusPagamento { get; set; } = StatusPagamentoOrdemServico.NaoPago;
    public StatusAprovacaoOrdemServico StatusAprovacao { get; set; } = StatusAprovacaoOrdemServico.Pendente;

    public string? DescricaoProblema { get; set; }
    public string? Observacoes { get; set; }
    public string? MotivoCancelamento { get; set; }

    /// <summary>Código opaco do link público de acompanhamento (sem login).</summary>
    public required string CodigoAcompanhamento { get; set; }

    /// <summary>Idempotency-Key da criação: reenvio devolve a mesma OS.</summary>
    public string? ChaveIdempotencia { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public List<OrdemServicoHistoricoEtapa> Historico { get; set; } = [];
}
