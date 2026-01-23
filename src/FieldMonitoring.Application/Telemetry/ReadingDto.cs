using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// DTO para representação de leituras em respostas da API.
/// </summary>
public sealed record ReadingDto
{
    /// <summary>
    /// Timestamp da leitura.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Identificador do sensor que gerou a leitura.
    /// </summary>
    public required string SensorId { get; init; }

    /// <summary>
    /// Percentual de umidade do solo.
    /// </summary>
    public required double SoilHumidity { get; init; }

    /// <summary>
    /// Temperatura do solo em graus Celsius.
    /// </summary>
    public required double SoilTemperature { get; init; }

    /// <summary>
    /// Temperatura do ar em graus Celsius.
    /// </summary>
    public double? AirTemperature { get; init; }

    /// <summary>
    /// Percentual de umidade do ar.
    /// </summary>
    public double? AirHumidity { get; init; }

    /// <summary>
    /// Precipitação em milímetros.
    /// </summary>
    public required double RainMm { get; init; }

    /// <summary>
    /// Mapeia um SensorReading de domínio para ReadingDto.
    /// Extrai os valores primitivos dos Value Objects.
    /// </summary>
    public static ReadingDto FromSensorReading(SensorReading reading)
    {
        return new ReadingDto
        {
            Timestamp = reading.Timestamp,
            SensorId = reading.SensorId,
            SoilHumidity = reading.SoilMoisture.Percent,
            SoilTemperature = reading.SoilTemperature.Celsius,
            AirTemperature = reading.AirTemperature?.Celsius,
            AirHumidity = reading.AirHumidity?.Percent,
            RainMm = reading.Rain.Millimeters
        };
    }
}
