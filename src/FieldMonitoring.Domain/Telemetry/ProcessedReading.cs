namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Representa um registro de leitura processada para controle de idempotência.
/// Evita que a mesma leitura seja processada mais de uma vez.
/// </summary>
public class ProcessedReading
{
    /// <summary>
    /// Identificador único da leitura (Chave Primária).
    /// </summary>
    public required string ReadingId { get; set; }

    /// <summary>
    /// Identificador do talhão ao qual a leitura pertence.
    /// </summary>
    public required string FieldId { get; set; }

    /// <summary>
    /// Timestamp de quando a leitura foi processada.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Origem da leitura (HTTP ou MQTT).
    /// </summary>
    public ReadingSource Source { get; set; }
}
