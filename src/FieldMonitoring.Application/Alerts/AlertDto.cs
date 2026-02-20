using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// DTO para representacao de alertas em respostas da API.
/// </summary>
public sealed record AlertDto
{
    public required Guid AlertId { get; init; }
    public required string FarmId { get; init; }
    public required string FieldId { get; init; }
    public required AlertType AlertType { get; init; }
    public string AlertTypeName => AlertType.ToString();
    public int? Severity { get; init; }
    public required AlertStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public string? Reason { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>
    /// Mapeia um Alert de dominio para AlertDto.
    /// </summary>
    public static AlertDto FromEntity(Alert alert)
    {
        return new AlertDto
        {
            AlertId = alert.AlertId,
            FarmId = alert.FarmId,
            FieldId = alert.FieldId,
            AlertType = alert.AlertType,
            Severity = alert.Severity,
            Status = alert.Status,
            Reason = alert.Reason,
            StartedAt = alert.StartedAt,
            ResolvedAt = alert.ResolvedAt
        };
    }
}
