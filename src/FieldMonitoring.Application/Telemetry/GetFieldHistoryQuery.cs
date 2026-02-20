using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Query para obter histórico de leituras de um talhão.
/// </summary>
public class GetFieldHistoryQuery
{
    private readonly ITimeSeriesReadingsStore _timeSeriesStore;

    public GetFieldHistoryQuery(ITimeSeriesReadingsStore timeSeriesStore)
    {
        _timeSeriesStore = timeSeriesStore;
    }

    /// <summary>
    /// Executa a query para obter leituras brutas.
    /// </summary>
    public async Task<IReadOnlyList<ReadingDto>> ExecuteAsync(
        string fieldId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        IReadOnlyList<SensorReading> readings = await _timeSeriesStore.GetByPeriodAsync(fieldId, from, to, cancellationToken);
        return readings.Select(ReadingDto.FromSensorReading).ToList();
    }

}
