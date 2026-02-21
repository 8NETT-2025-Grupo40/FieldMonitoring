namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO para visão geral de talhão no dashboard (resumo por talhão).
/// </summary>
public sealed record FieldOverviewDto : FieldSummaryDto
{
    /// <summary>
    /// Número de alertas ativos para este talhão.
    /// </summary>
    public int ActiveAlertCount { get; init; }
}
