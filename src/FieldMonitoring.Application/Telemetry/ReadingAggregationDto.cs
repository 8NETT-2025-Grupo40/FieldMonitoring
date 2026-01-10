using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// DTO para representação de leituras agregadas em respostas da API.
/// </summary>
public sealed record ReadingAggregationDto
{
    /// <summary>
    /// Início do intervalo de tempo.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Percentual médio de umidade do solo.
    /// </summary>
    public required double AvgSoilHumidity { get; init; }

    /// <summary>
    /// Percentual mínimo de umidade do solo.
    /// </summary>
    public required double MinSoilHumidity { get; init; }

    /// <summary>
    /// Percentual máximo de umidade do solo.
    /// </summary>
    public required double MaxSoilHumidity { get; init; }

    /// <summary>
    /// Temperatura média do solo em Celsius.
    /// </summary>
    public required double AvgSoilTemperature { get; init; }

    /// <summary>
    /// Temperatura mínima do solo em Celsius.
    /// </summary>
    public required double MinSoilTemperature { get; init; }

    /// <summary>
    /// Temperatura máxima do solo em Celsius.
    /// </summary>
    public required double MaxSoilTemperature { get; init; }

    /// <summary>
    /// Temperatura média do ar em Celsius.
    /// </summary>
    public double? AvgAirTemperature { get; init; }

    /// <summary>
    /// Temperatura mínima do ar em Celsius.
    /// </summary>
    public double? MinAirTemperature { get; init; }

    /// <summary>
    /// Temperatura máxima do ar em Celsius.
    /// </summary>
    public double? MaxAirTemperature { get; init; }

    /// <summary>
    /// Percentual médio de umidade do ar.
    /// </summary>
    public double? AvgAirHumidity { get; init; }

    /// <summary>
    /// Percentual mínimo de umidade do ar.
    /// </summary>
    public double? MinAirHumidity { get; init; }

    /// <summary>
    /// Percentual máximo de umidade do ar.
    /// </summary>
    public double? MaxAirHumidity { get; init; }

    /// <summary>
    /// Total de precipitação em milímetros.
    /// </summary>
    public required double TotalRainMm { get; init; }

    /// <summary>
    /// Número de leituras agregadas.
    /// </summary>
    public required int ReadingCount { get; init; }

    /// <summary>
    /// Mapeia uma ReadingAggregation do domínio para DTO.
    /// </summary>
    public static ReadingAggregationDto FromAggregation(ReadingAggregation aggregation)
    {
        return new ReadingAggregationDto
        {
            Timestamp = aggregation.Timestamp,
            AvgSoilHumidity = aggregation.AvgSoilHumidity,
            MinSoilHumidity = aggregation.MinSoilHumidity,
            MaxSoilHumidity = aggregation.MaxSoilHumidity,
            AvgSoilTemperature = aggregation.AvgSoilTemperature,
            MinSoilTemperature = aggregation.MinSoilTemperature,
            MaxSoilTemperature = aggregation.MaxSoilTemperature,
            AvgAirTemperature = aggregation.AvgAirTemperature,
            MinAirTemperature = aggregation.MinAirTemperature,
            MaxAirTemperature = aggregation.MaxAirTemperature,
            AvgAirHumidity = aggregation.AvgAirHumidity,
            MinAirHumidity = aggregation.MinAirHumidity,
            MaxAirHumidity = aggregation.MaxAirHumidity,
            TotalRainMm = aggregation.TotalRainMm,
            ReadingCount = aggregation.ReadingCount
        };
    }
}
