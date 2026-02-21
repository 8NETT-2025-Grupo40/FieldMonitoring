using FieldMonitoring.Domain;
using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Contrato de mensagem para telemetria recebida do TelemetryIntake via SQS.
/// </summary>
public sealed record TelemetryReceivedMessage
{
    private const string SourceHttp = "http";
    private const string SourceMqtt = "mqtt";

    public required string ReadingId { get; init; }
    public required string SensorId { get; init; }
    public required string FieldId { get; init; }
    public required string FarmId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required double SoilHumidity { get; init; }
    public required double SoilTemperature { get; init; }

    // Opcional - nem todos os sensores possuem este dado.
    public double? AirTemperature { get; init; }

    // Opcional - nem todos os sensores possuem este dado.
    public double? AirHumidity { get; init; }

    public required double RainMm { get; init; }
    public string Source { get; init; } = SourceHttp;

    /// <summary>
    /// Converte a mensagem para um SensorReading de dominio.
    /// </summary>
    public Result<SensorReading> ToSensorReading()
    {
        ReadingSource source = Source?.ToLowerInvariant() switch
        {
            SourceMqtt => ReadingSource.Mqtt,
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
