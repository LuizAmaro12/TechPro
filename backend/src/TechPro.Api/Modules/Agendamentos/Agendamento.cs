using System.Text.Json.Serialization;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

// Como string no JSON também fora do pipeline da API (ex.: clientes de teste
// que desserializam com as opções padrão, sem os converters do servidor).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusAgendamento
{
    Agendado,
    CheckInRealizado,
    Cancelado,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrigemAgendamento
{
    Manual,
    Portal,
}

/// <summary>
/// Um horário reservado na agenda da loja. Guarda snapshot do contato e do
/// aparelho informados (o portal público não exige login), com vínculo
/// opcional a um Cliente do CRM. A conversão automática em OS chega na etapa
/// do módulo de OS — o status CheckInRealizado é o gancho.
/// </summary>
public class Agendamento : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public DateOnly Data { get; set; }
    public TimeOnly HoraInicio { get; set; }

    /// <summary>Fim alinhado à grade de 30 min: início + ceil(duração/30)*30.</summary>
    public TimeOnly HoraFim { get; set; }

    public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;
    public OrigemAgendamento Origem { get; set; }

    public required string NomeContato { get; set; }
    public required string TelefoneContato { get; set; }
    public string? EmailContato { get; set; }
    public string? DescricaoProblema { get; set; }
    public string? AparelhoMarca { get; set; }
    public string? AparelhoModelo { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? ReagendadoEm { get; set; }
    public DateTimeOffset? CanceladoEm { get; set; }
    public string? MotivoCancelamento { get; set; }
}
