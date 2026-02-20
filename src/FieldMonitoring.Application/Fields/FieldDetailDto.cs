using FieldMonitoring.Application.Alerts;

namespace FieldMonitoring.Application.Fields;

/// <summary>
/// DTO para informações detalhadas de um talhão.
/// </summary>
public sealed record FieldDetailDto : FieldSummaryDto
{
    /// <summary>
    /// Alertas ativos para este talhão.
    /// </summary>
    public IReadOnlyList<AlertDto> ActiveAlerts { get; init; } = [];

    /// <summary>
    /// Timestamp de quando o status foi atualizado pela última vez.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
