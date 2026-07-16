namespace TechPro.Api.Modules.Onboarding.Dtos;

/// <summary>Passos do checklist de ativação, derivados dos dados reais.</summary>
public record PassosOnboarding(
    bool LojaConfigurada,
    bool HorariosConfigurados,
    bool TemServico,
    bool TemPeca,
    bool TemCliente);

public record OnboardingStatusResponse(
    bool OnboardingConcluido,
    PassosOnboarding Passos,
    int PassosConcluidos,
    int TotalPassos,
    bool TemDadosExemplo);
