namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Representa dados agregados de leitura para um intervalo de tempo (hora/dia).
/// Usado para gráficos no dashboard para reduzir volume de dados.
/// </summary>
public sealed record ReadingAggregation
{
    /// <summary>
    /// Início do intervalo de tempo.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Percentual médio de umidade do solo no intervalo.
    /// </summary>
    public double AvgSoilHumidity { get; init; }

    /// <summary>
    /// Percentual mínimo de umidade do solo no intervalo.
    /// </summary>
    public double MinSoilHumidity { get; init; }

    /// <summary>
    /// Percentual máximo de umidade do solo no intervalo.
    /// </summary>
    public double MaxSoilHumidity { get; init; }

    /// <summary>
    /// Temperatura média do solo em Celsius no intervalo.
    /// </summary>
    public double AvgSoilTemperature { get; init; }

    /// <summary>
    /// Temperatura mínima do solo em Celsius no intervalo.
    /// </summary>
    public double MinSoilTemperature { get; init; }

    /// <summary>
    /// Temperatura máxima do solo em Celsius no intervalo.
    /// </summary>
    public double MaxSoilTemperature { get; init; }

    /// <summary>
    /// Temperatura média do ar em Celsius no intervalo.
    /// </summary>
    public double? AvgAirTemperature { get; init; }

    /// <summary>
    /// Temperatura mínima do ar em Celsius no intervalo.
    /// </summary>
    public double? MinAirTemperature { get; init; }

    /// <summary>
    /// Temperatura máxima do ar em Celsius no intervalo.
    /// </summary>
    public double? MaxAirTemperature { get; init; }

    /// <summary>
    /// Percentual médio de umidade do ar no intervalo.
    /// </summary>
    public double? AvgAirHumidity { get; init; }

    /// <summary>
    /// Percentual mínimo de umidade do ar no intervalo.
    /// </summary>
    public double? MinAirHumidity { get; init; }

    /// <summary>
    /// Percentual máximo de umidade do ar no intervalo.
    /// </summary>
    public double? MaxAirHumidity { get; init; }

    /// <summary>
    /// Total de precipitação em milímetros no intervalo.
    /// </summary>
    public double TotalRainMm { get; init; }

    /// <summary>
    /// Número de leituras agregadas neste intervalo.
    /// </summary>
    public int ReadingCount { get; init; }
}
