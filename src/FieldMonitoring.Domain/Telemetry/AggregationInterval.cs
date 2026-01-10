namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Representa o intervalo de agregação para queries de séries temporais.
/// </summary>
public enum AggregationInterval
{
    /// <summary>
    /// Sem agregação, retorna leituras brutas.
    /// </summary>
    None = 0,

    /// <summary>
    /// Agrega por hora.
    /// </summary>
    Hour = 1,

    /// <summary>
    /// Agrega por dia.
    /// </summary>
    Day = 2
}
