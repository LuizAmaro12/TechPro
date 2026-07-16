namespace TechPro.Api.Modules.Dashboard.Dtos;

public record OsAtrasadaResponse(
    Guid Id,
    int Numero,
    string ClienteNome,
    string ServicoNome,
    DateOnly PrazoEstimado,
    int DiasAtraso);

public record OrcamentoPendenteResponse(
    Guid Id,
    int Numero,
    string ClienteNome,
    decimal Total,
    DateTimeOffset EnviadoEm,
    int DiasAguardando);

/// <summary>
/// "Radar do dia": o que precisa de atenção agora. Listas limitadas (o total
/// sinaliza quando há mais do que o exibido) e ordenadas pela maior urgência.
/// </summary>
public record RadarResponse(
    List<OsAtrasadaResponse> OsAtrasadas,
    int TotalOsAtrasadas,
    List<OrcamentoPendenteResponse> OrcamentosPendentes,
    int TotalOrcamentosPendentes);

public record DashboardResponse(
    int OsAbertas,
    int AgendamentosHoje,
    int ServicosEmAtraso,
    int AparelhosEmReparo,
    int ProntosParaRetirada,
    decimal FaturamentoMes,
    decimal FaturamentoMesAnterior,
    decimal? VariacaoFaturamentoPct,
    RadarResponse Radar);
