using System.Text.Json.Serialization;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusFilaEspera
{
    Aguardando,
    Convertida,
    Descartada,
}

/// <summary>
/// Demanda capturada quando não havia horário na data desejada — receita que
/// antes se perdia. Não ocupa horário nem vira OS por si só; é uma entidade à
/// parte do <see cref="Agendamento"/> justamente para não sujar as regras de
/// disponibilidade/capacidade. Convertê-la cria um agendamento normal.
/// </summary>
public class EntradaFilaEspera : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public required string NomeContato { get; set; }
    public required string TelefoneContato { get; set; }
    public string? EmailContato { get; set; }

    /// <summary>Data que o cliente queria e não tinha vaga — só informativa.</summary>
    public DateOnly? DataPreferida { get; set; }

    public string? DescricaoProblema { get; set; }
    public string? AparelhoMarca { get; set; }
    public string? AparelhoModelo { get; set; }

    public OrigemAgendamento Origem { get; set; }
    public StatusFilaEspera Status { get; set; } = StatusFilaEspera.Aguardando;

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? ResolvidaEm { get; set; }

    /// <summary>Agendamento gerado na conversão.</summary>
    public int? AgendamentoId { get; set; }

    public string? MotivoDescarte { get; set; }
}
