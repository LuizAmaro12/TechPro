using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Horário de funcionamento da loja em um dia da semana. Dia sem registro ou
/// com Ativo=false é dia fechado (fail-closed: sem configuração, sem slots).
/// Horários são "hora de parede" da loja (TimeOnly) — agenda não usa timezone.
/// </summary>
public class HorarioFuncionamento : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>0=domingo .. 6=sábado (mesma semântica de DayOfWeek).</summary>
    public int DiaSemana { get; set; }

    public TimeOnly Abertura { get; set; }
    public TimeOnly Fechamento { get; set; }

    /// <summary>Pausa opcional (ex.: almoço) — ambos preenchidos ou nenhum.</summary>
    public TimeOnly? IntervaloInicio { get; set; }
    public TimeOnly? IntervaloFim { get; set; }

    public bool Ativo { get; set; } = true;
}
