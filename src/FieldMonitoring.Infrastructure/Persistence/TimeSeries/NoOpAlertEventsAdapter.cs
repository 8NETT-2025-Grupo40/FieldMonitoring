using FieldMonitoring.Application.Alerts;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Adapter no-op para eventos de alerta quando o InfluxDB está desabilitado.
/// </summary>
public sealed class NoOpAlertEventsAdapter : IAlertEventsStore
{
    public Task AppendAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
