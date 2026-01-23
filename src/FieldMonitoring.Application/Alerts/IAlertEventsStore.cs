namespace FieldMonitoring.Application.Alerts;

/// <summary>
/// Port para gravação de eventos de alerta em time-series.
/// </summary>
public interface IAlertEventsStore
{
    Task AppendAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default);
}
