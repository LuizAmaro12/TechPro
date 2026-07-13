using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Bloqueio pontual de agenda (feriado, ausência, manutenção): nenhum slot é
/// oferecido dentro do intervalo. Configuração operacional — exclusão física
/// permitida (a regra "exclusão = desativação" vale para registros de negócio).
/// </summary>
public class BloqueioAgenda : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public string? Motivo { get; set; }
}
