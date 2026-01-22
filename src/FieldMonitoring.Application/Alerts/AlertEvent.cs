using FieldMonitoring.Domain.Alerts;

namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Evento de alerta projetado para time-series (InfluxDB).
/// Representa uma mudanca de status do alerta.
/// </summary>
public sealed record AlertEvent
{
    public required Guid AlertId { get; init; }
    public required string FarmId { get; init; }
    public required string FieldId { get; init; }
    public required AlertType AlertType { get; init; }
    public required AlertStatus Status { get; init; }
    public string? Reason { get; init; }
    public int? Severity { get; init; }
    public required DateTime OccurredAt { get; init; }
}
