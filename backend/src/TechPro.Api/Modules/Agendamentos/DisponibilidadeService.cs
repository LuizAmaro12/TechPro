using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Modules.Agendamentos;

/// <summary>
/// Calcula os horários livres de um serviço em uma data: grade de slots de
/// 30 min dentro do horário de funcionamento, menos intervalo, bloqueios e
/// slots em que a CapacidadeSimultanea do serviço já foi atingida.
/// Aritmética em minutos inteiros — TimeOnly.AddMinutes dá a volta na
/// meia-noite e quebraria as comparações.
/// </summary>
public class DisponibilidadeService(TechProDbContext db)
{
    public const int MinutosPorSlot = 30;

    public async Task<CatalogoResultado<DisponibilidadeResponse>> CalcularAsync(
        int servicoId,
        DateOnly data,
        bool somenteAgendaveisOnline = false,
        int? ignorarAgendamentoId = null)
    {
        var servico = await db.Servicos
            .FirstOrDefaultAsync(s => s.Id == servicoId && s.Ativo);
        if (servico is null || (somenteAgendaveisOnline && !servico.AgendavelOnline))
        {
            return CatalogoResultado<DisponibilidadeResponse>.Falha("Serviço não encontrado.");
        }

        var duracaoOcupada = DuracaoOcupadaMinutos(servico.DuracaoEstimadaMinutos);

        var horario = await db.HorariosFuncionamento
            .FirstOrDefaultAsync(h => h.DiaSemana == (int)data.DayOfWeek && h.Ativo);
        if (horario is null)
        {
            // Dia fechado (ou semana ainda não configurada): sem slots.
            return CatalogoResultado<DisponibilidadeResponse>.Ok(
                new DisponibilidadeResponse(data, servicoId, duracaoOcupada, []));
        }

        var bloqueios = await db.BloqueiosAgenda
            .Where(b => b.Data == data)
            .Select(b => new { b.HoraInicio, b.HoraFim })
            .ToListAsync();

        var ocupados = await db.Agendamentos
            .Where(a => a.Data == data
                && a.ServicoId == servicoId
                && a.Status != StatusAgendamento.Cancelado
                && (ignorarAgendamentoId == null || a.Id != ignorarAgendamentoId))
            .Select(a => new { a.HoraInicio, a.HoraFim })
            .ToListAsync();

        var abertura = Minutos(horario.Abertura);
        var fechamento = Minutos(horario.Fechamento);
        var livres = new List<TimeOnly>();

        for (var inicio = abertura; inicio + duracaoOcupada <= fechamento; inicio += MinutosPorSlot)
        {
            var fim = inicio + duracaoOcupada;

            if (horario.IntervaloInicio is { } intervaloInicio
                && horario.IntervaloFim is { } intervaloFim
                && Sobrepoe(inicio, fim, Minutos(intervaloInicio), Minutos(intervaloFim)))
            {
                continue;
            }

            if (bloqueios.Any(b => Sobrepoe(inicio, fim, Minutos(b.HoraInicio), Minutos(b.HoraFim))))
            {
                continue;
            }

            // Capacidade: em cada sub-slot de 30 min da janela, o número de
            // agendamentos simultâneos do serviço não pode atingir o limite.
            var temVaga = true;
            for (var sub = inicio; sub < fim && temVaga; sub += MinutosPorSlot)
            {
                var simultaneos = ocupados.Count(a =>
                    Sobrepoe(sub, sub + MinutosPorSlot, Minutos(a.HoraInicio), Minutos(a.HoraFim)));
                temVaga = simultaneos < servico.CapacidadeSimultanea;
            }

            if (temVaga)
            {
                livres.Add(new TimeOnly(inicio / 60, inicio % 60));
            }
        }

        return CatalogoResultado<DisponibilidadeResponse>.Ok(
            new DisponibilidadeResponse(data, servicoId, duracaoOcupada, livres));
    }

    /// <summary>Um serviço ocupa slots inteiros: ceil(duração/30)*30 minutos.</summary>
    public static int DuracaoOcupadaMinutos(int duracaoEstimadaMinutos) =>
        Math.Max(1, (duracaoEstimadaMinutos + MinutosPorSlot - 1) / MinutosPorSlot) * MinutosPorSlot;

    private static int Minutos(TimeOnly hora) => hora.Hour * 60 + hora.Minute;

    private static bool Sobrepoe(int aInicio, int aFim, int bInicio, int bFim) =>
        aInicio < bFim && bInicio < aFim;
}
