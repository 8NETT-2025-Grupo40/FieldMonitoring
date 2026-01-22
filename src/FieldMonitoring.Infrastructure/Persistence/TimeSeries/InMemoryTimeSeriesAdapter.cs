using System.Collections.Concurrent;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Implementação in-memory do ITimeSeriesReadingsStore para MVP/testes.
/// Substituir por implementação InfluxDB
/// </summary>
public class InMemoryTimeSeriesAdapter : ITimeSeriesReadingsStore
{
    private readonly ConcurrentDictionary<string, List<SensorReading>> _readings = new();
    private readonly object _lock = new();

    public Task AppendAsync(SensorReading reading, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            List<SensorReading> list = _readings.GetOrAdd(reading.FieldId, _ => new List<SensorReading>());
            list.Add(reading);
            // Keep sorted by timestamp
            list.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SensorReading>> GetByPeriodAsync(
        string fieldId,
        DateTime from,
        DateTime to,
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
