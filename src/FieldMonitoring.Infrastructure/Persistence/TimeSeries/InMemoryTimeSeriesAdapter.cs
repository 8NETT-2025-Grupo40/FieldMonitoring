using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Implementação in-memory do ITimeSeriesReadingsStore para MVP/testes.
/// Substituir por implementação InfluxDB em produção.
/// </summary>
public class InMemoryTimeSeriesAdapter : ITimeSeriesReadingsStore
{
    private readonly Dictionary<string, List<SensorReading>> _readings = new();
    private readonly object _lock = new();

    public Task AppendAsync(SensorReading reading, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_readings.TryGetValue(reading.FieldId, out List<SensorReading>? list))
            {
                list = new List<SensorReading>();
                _readings[reading.FieldId] = list;
            }
            list.Add(reading);
            // Mantém ordenado por timestamp
            list.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SensorReading>> GetByPeriodAsync(
        string fieldId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_readings.TryGetValue(fieldId, out List<SensorReading>? list))
            {
                return Task.FromResult<IReadOnlyList<SensorReading>>(Array.Empty<SensorReading>());
            }

            List<SensorReading> result = list
                .Where(r => r.Timestamp >= from && r.Timestamp <= to)
                .OrderBy(r => r.Timestamp)
                .ToList();

            return Task.FromResult<IReadOnlyList<SensorReading>>(result);
        }
    }

    /// <summary>
    /// Limpa todas as leituras (para testes).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _readings.Clear();
        }
    }
}
