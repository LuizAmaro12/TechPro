using Hangfire;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Agenda o lembrete temporizado de um agendamento. Abstraído para que os
/// testes (e o `dotnet run` puro, sem Hangfire) não dependam de Postgres:
/// sem a flag Comunicacao:Hangfire:Habilitado, entra o adaptador nulo.
/// </summary>
public interface IAgendadorDeLembretes
{
    void AgendarLembrete(int agendamentoId, DateTimeOffset quando, Guid tenantId);
}

/// <summary>Sem Hangfire ligado: não agenda nada (as notificações imediatas seguem).</summary>
public sealed class AgendadorDeLembretesNulo : IAgendadorDeLembretes
{
    public void AgendarLembrete(int agendamentoId, DateTimeOffset quando, Guid tenantId)
    {
    }
}

public sealed class HangfireAgendadorDeLembretes(IBackgroundJobClient jobs) : IAgendadorDeLembretes
{
    public void AgendarLembrete(int agendamentoId, DateTimeOffset quando, Guid tenantId) =>
        jobs.Schedule<LembreteJob>(job => job.ExecutarAsync(agendamentoId, tenantId), quando);
}

/// <summary>
/// Job Hangfire do lembrete: roda fora do HTTP, então fixa o tenant via
/// <see cref="TenantAmbiente"/> (mesmo padrão das rotas públicas) antes de
/// tocar o banco — GQF e RLS passam a valer normalmente.
/// </summary>
public sealed class LembreteJob(TenantAmbiente tenantAmbiente, ComunicacaoService comunicacao)
{
    public async Task ExecutarAsync(int agendamentoId, Guid tenantId)
    {
        tenantAmbiente.TenantIdFixado = tenantId;
        await comunicacao.NotificarLembreteAgendamentoAsync(agendamentoId);
    }
}
