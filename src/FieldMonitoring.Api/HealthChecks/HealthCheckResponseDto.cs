namespace FieldMonitoring.Api.HealthChecks;

public sealed record HealthCheckResponseDto(
    string Status,
    DateTimeOffset Timestamp,
    Dictionary<string, HealthCheckEntryDto> Checks);

public sealed record HealthCheckEntryDto(
    string Status,
    string? Description,
    double DurationMs);
