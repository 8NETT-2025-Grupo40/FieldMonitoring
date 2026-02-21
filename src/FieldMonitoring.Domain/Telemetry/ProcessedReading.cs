namespace FieldMonitoring.Domain.Telemetry;

/// <summary>
/// Representa um registro de leitura processada para controle de idempotência.
/// Evita que a mesma leitura seja processada mais de uma vez.
/// </summary>
public class ProcessedReading
{
    /// <summary>
    /// Construtor privado para reidratação pelo EF Core.
    /// </summary>
    private ProcessedReading() { }

    /// <summary>
    /// Identificador único da leitura (Chave Primária).
    /// </summary>
    public string ReadingId { get; private set; } = null!;

    /// <summary>
    /// Identificador do talhão ao qual a leitura pertence.
    /// </summary>
    public string FieldId { get; private set; } = null!;

    /// <summary>
    /// Timestamp de quando a leitura foi processada.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Origem da leitura (HTTP ou MQTT).
    /// </summary>
    public ReadingSource Source { get; private set; }

    /// <summary>
    /// Cria um novo registro de leitura processada.
    /// </summary>
    /// <param name="readingId">Identificador único da leitura.</param>
    /// <param name="fieldId">Identificador do talhão.</param>
    /// <param name="source">Origem da leitura.</param>
    public static ProcessedReading Create(string readingId, string fieldId, ReadingSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        return new ProcessedReading
        {
            ReadingId = readingId,
            FieldId = fieldId,
            ProcessedAt = DateTimeOffset.UtcNow,
            Source = source
        };
    }
}
