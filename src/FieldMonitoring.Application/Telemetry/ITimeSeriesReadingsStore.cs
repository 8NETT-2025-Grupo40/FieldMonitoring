using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Port para operações no banco de dados de séries temporais (histórico de leituras).
/// Abstração que permite trocar entre InfluxDB, MongoDB ou in-memory.
/// </summary>
public interface ITimeSeriesReadingsStore
{
    /// <summary>
    /// Adiciona uma nova leitura de sensor ao banco de séries temporais.
    /// </summary>
    Task AppendAsync(SensorReading reading, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém leituras brutas de um talhão dentro de um período.
    /// </summary>
    Task<IReadOnlyList<SensorReading>> GetByPeriodAsync(
        string fieldId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém leituras agregadas de um talhão dentro de um período.
    /// </summary>
    Task<IReadOnlyList<ReadingAggregation>> GetAggregatedAsync(
        string fieldId,
        DateTime from,
        DateTime to,
        AggregationInterval interval,
        CancellationToken cancellationToken = default);
}
