using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.HealthChecks;

internal static class HealthCheckResponseWriter
{
    public static IResult ToResult(HealthReport report, HealthCheckEndpointType endpointType)
    {
        int statusCode = endpointType switch
        {
            HealthCheckEndpointType.Liveness => report.Status is HealthStatus.Unhealthy
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status200OK,
            HealthCheckEndpointType.Readiness => report.Status is HealthStatus.Healthy
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status503ServiceUnavailable
        };

        HealthCheckResponseDto payload = new(
            report.Status.ToString(),
            DateTimeOffset.UtcNow,
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckEntryDto(
                    entry.Value.Status.ToString(),
                    entry.Value.Description,
                    entry.Value.Duration.TotalMilliseconds)));

        return Results.Json(payload, statusCode: statusCode);
    }
}
