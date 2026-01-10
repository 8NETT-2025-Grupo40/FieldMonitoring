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
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SensorReading> readings = await _timeSeriesStore.GetByPeriodAsync(fieldId, from, to, cancellationToken);
        return readings.Select(ReadingDto.FromSensorReading).ToList();
    }

    /// <summary>
    /// Executa a query para obter leituras agregadas.
    /// </summary>
    public async Task<IReadOnlyList<ReadingAggregationDto>> ExecuteAggregatedAsync(
        string fieldId,
        DateTime from,
        DateTime to,
        AggregationInterval interval,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReadingAggregation> aggregations = await _timeSeriesStore.GetAggregatedAsync(fieldId, from, to, interval, cancellationToken);
        return aggregations.Select(ReadingAggregationDto.FromAggregation).ToList();
    }
}
