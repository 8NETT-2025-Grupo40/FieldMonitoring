namespace FieldMonitoring.Api.HealthChecks;

/// <summary>
/// Resposta padrão dos endpoints de health check.
/// </summary>
/// <param name="Status">Status agregado da aplicação.</param>
/// <param name="Timestamp">Momento da avaliação.</param>
/// <param name="Checks">Resultado individual de cada verificação.</param>
public sealed record HealthCheckResponseDto(
    string Status,
    DateTimeOffset Timestamp,
    Dictionary<string, HealthCheckEntryDto> Checks);

/// <summary>
/// Resultado de uma verificação individual de saúde.
/// </summary>
/// <param name="Status">Status da verificação.</param>
/// <param name="Description">Descrição adicional da verificação.</param>
/// <param name="DurationMs">Duração em milissegundos.</param>
public sealed record HealthCheckEntryDto(
    string Status,
    string? Description,
    double DurationMs);
