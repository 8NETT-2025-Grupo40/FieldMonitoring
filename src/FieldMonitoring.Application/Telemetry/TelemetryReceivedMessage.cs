using FieldMonitoring.Domain;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Contrato de mensagem para telemetria recebida do TelemetryIntake via SQS.
/// </summary>
public sealed record TelemetryReceivedMessage
{
    /// <summary>
    /// Identificador único para controle de idempotência.
    /// </summary>
    public required string ReadingId { get; init; }

    /// <summary>
    /// Identificador do sensor que gerou a leitura.
    /// </summary>
    public required string SensorId { get; init; }

    /// <summary>
    /// Identificador do talhão onde a leitura foi realizada.
    /// </summary>
    public required string FieldId { get; init; }

    /// <summary>
    /// Identificador da fazenda à qual o talhão pertence.
    /// </summary>
    public required string FarmId { get; init; }

    /// <summary>
    /// Timestamp de quando a medição foi realizada (ISO 8601 com offset).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Percentual de umidade do solo (0-100).
    /// </summary>
    public required double SoilHumidity { get; init; }

    /// <summary>
    /// Temperatura do solo em graus Celsius.
    /// </summary>
    public required double SoilTemperature { get; init; }

    /// <summary>
    /// Temperatura do ar em graus Celsius.
    /// Opcional - nem todos os sensores possuem este dado.
    /// </summary>
    public double? AirTemperature { get; init; }

    /// <summary>
    /// Percentual de umidade do ar (0-100).
    /// Opcional - nem todos os sensores possuem este dado.
    /// </summary>
    public double? AirHumidity { get; init; }

    /// <summary>
    /// Precipitação em milímetros.
    /// </summary>
    public required double RainMm { get; init; }

    /// <summary>
    /// Origem da leitura ("http" ou "mqtt").
    /// </summary>
    public string Source { get; init; } = "http";

    /// <summary>
    /// Converte a mensagem para um SensorReading de domínio.
    /// Valida e cria Value Objects a partir dos primitivos.
    /// </summary>
    public Result<SensorReading> ToSensorReading()
    {
        ReadingSource source = Source?.ToLowerInvariant() switch
        {
            "mqtt" => ReadingSource.Mqtt,
            _ => ReadingSource.Http
        };

        return SensorReading.Create(
            readingId: ReadingId,
            sensorId: SensorId,
            fieldId: FieldId,
            farmId: FarmId,
            timestamp: Timestamp,
            soilMoisturePercent: SoilHumidity,
            soilTemperatureC: SoilTemperature,
            rainMm: RainMm,
            airTemperatureC: AirTemperature,
            airHumidityPercent: AirHumidity,
            source: source);
    }
}
