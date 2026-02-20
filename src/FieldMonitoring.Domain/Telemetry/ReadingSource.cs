namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Representa a origem de uma leitura de sensor.
/// </summary>
public enum ReadingSource
{
    /// <summary>
    /// Leitura recebida via API HTTP.
    /// </summary>
    Http = 1,

    /// <summary>
    /// Leitura recebida via protocolo MQTT.
    /// </summary>
    Mqtt = 2
}
