namespace TechPro.Api.Modules.Agendamentos.Dtos;

// --- Horários de funcionamento ----------------------------------------------

public record HorarioFuncionamentoDia(
    int DiaSemana,
    bool Ativo,
    TimeOnly? Abertura,
    TimeOnly? Fechamento,
    TimeOnly? IntervaloInicio,
    TimeOnly? IntervaloFim);

/// <summary>Sempre os 7 dias, num único PUT — configuração atômica da semana.</summary>
public record HorariosFuncionamentoRequest(List<HorarioFuncionamentoDia> Dias);

// --- Bloqueios de agenda -----------------------------------------------------

public record BloqueioRequest(
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    string? Motivo);

public record BloqueioResponse(
    int Id,
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    string? Motivo);

// --- Configurações (slug público) -------------------------------------------

public record ConfiguracaoAgendaRequest(string Slug);

public record ConfiguracaoAgendaResponse(string Slug);

// --- Disponibilidade ---------------------------------------------------------

public record DisponibilidadeResponse(
    DateOnly Data,
    int ServicoId,
    int DuracaoMinutos,
    List<TimeOnly> HorariosLivres);
