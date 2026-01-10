namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Represents the source of a sensor reading.
/// </summary>
public enum ReadingSource
{
    /// <summary>
    /// Reading received via HTTP API.
    /// </summary>
    Http = 1,

    /// <summary>
    /// Reading received via MQTT protocol.
    /// </summary>
    Mqtt = 2
}
