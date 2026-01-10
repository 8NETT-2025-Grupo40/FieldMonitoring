using System.Collections.Concurrent;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Infrastructure.Persistence.TimeSeries;

/// <summary>
/// Implementação in-memory do ITimeSeriesReadingsStore para MVP/testes.
/// Substituir por implementação InfluxDB ou MongoDB para produção.
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

    public Task<IReadOnlyList<ReadingAggregation>> GetAggregatedAsync(
        string fieldId,
        DateTime from,
        DateTime to,
        AggregationInterval interval,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_readings.TryGetValue(fieldId, out List<SensorReading>? list))
            {
                return Task.FromResult<IReadOnlyList<ReadingAggregation>>(Array.Empty<ReadingAggregation>());
            }

            List<SensorReading> readings = list
                .Where(r => r.Timestamp >= from && r.Timestamp <= to)
                .ToList();

            if (readings.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<ReadingAggregation>>(Array.Empty<ReadingAggregation>());
            }

            IEnumerable<IGrouping<DateTime, SensorReading>> grouped = interval switch
            {
                AggregationInterval.Hour => readings.GroupBy(r => new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, r.Timestamp.Hour, 0, 0, DateTimeKind.Utc)),
                AggregationInterval.Day => readings.GroupBy(r => new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, 0, 0, 0, DateTimeKind.Utc)),
                _ => readings.GroupBy(r => r.Timestamp)
            };

            List<ReadingAggregation> result = grouped
                .Select(g => 
                {
                    // Calcula métricas de ar apenas se houver leituras com esses dados
                    var airTempReadings = g.Where(r => r.AirTemperature != null).ToList();
                    var airHumidityReadings = g.Where(r => r.AirHumidity != null).ToList();

                    return new ReadingAggregation
                    {
                        Timestamp = g.Key,
                        AvgSoilHumidity = g.Average(r => r.SoilMoisture.Percent),
                        MinSoilHumidity = g.Min(r => r.SoilMoisture.Percent),
                        MaxSoilHumidity = g.Max(r => r.SoilMoisture.Percent),
                        AvgSoilTemperature = g.Average(r => r.SoilTemperature.Celsius),
                        MinSoilTemperature = g.Min(r => r.SoilTemperature.Celsius),
                        MaxSoilTemperature = g.Max(r => r.SoilTemperature.Celsius),
                        AvgAirTemperature = airTempReadings.Count > 0 ? airTempReadings.Average(r => r.AirTemperature!.Celsius) : null,
                        MinAirTemperature = airTempReadings.Count > 0 ? airTempReadings.Min(r => r.AirTemperature!.Celsius) : null,
                        MaxAirTemperature = airTempReadings.Count > 0 ? airTempReadings.Max(r => r.AirTemperature!.Celsius) : null,
                        AvgAirHumidity = airHumidityReadings.Count > 0 ? airHumidityReadings.Average(r => r.AirHumidity!.Percent) : null,
                        MinAirHumidity = airHumidityReadings.Count > 0 ? airHumidityReadings.Min(r => r.AirHumidity!.Percent) : null,
                        MaxAirHumidity = airHumidityReadings.Count > 0 ? airHumidityReadings.Max(r => r.AirHumidity!.Percent) : null,
                        TotalRainMm = g.Sum(r => r.Rain.Millimeters),
                        ReadingCount = g.Count()
                    };
                })
                .OrderBy(a => a.Timestamp)
                .ToList();

            return Task.FromResult<IReadOnlyList<ReadingAggregation>>(result);
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
