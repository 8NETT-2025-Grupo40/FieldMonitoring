using FieldMonitoring.Domain.Telemetry;

namespace FieldMonitoring.Application.Telemetry;

/// <summary>
/// Port para controle de idempotência (rastreamento de leituras processadas).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Verifica se uma leitura já foi processada.
    /// </summary>
    Task<bool> ExistsAsync(string readingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca uma leitura como processada.
    /// </summary>
    Task MarkProcessedAsync(ProcessedReading processedReading, CancellationToken cancellationToken = default);
}
