using FieldMonitoring.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FieldMonitoring.Api.Extensions;

public static class HealthChecksEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/monitoring/health", async (
                HealthCheckService healthCheckService,
                CancellationToken cancellationToken) =>
            {
                HealthReport report = await healthCheckService.CheckHealthAsync(
                    registration => registration.Tags.Contains(HealthCheckTags.Live),
                    cancellationToken);

                return HealthCheckResponseWriter.ToResult(report, HealthCheckEndpointType.Liveness);
            })
            .WithName("MonitoringLiveness")
            .WithTags("Monitoring")
            .WithSummary("Liveness check")
            .WithDescription("Verifica se o processo da API está em execução.")
            .Produces<HealthCheckResponseDto>(StatusCodes.Status200OK)
            .Produces<HealthCheckResponseDto>(StatusCodes.Status503ServiceUnavailable)
            .AllowAnonymous();

        endpoints
            .MapGet("/monitoring/ready", async (
                HealthCheckService healthCheckService,
                CancellationToken cancellationToken) =>
            {
                HealthReport report = await healthCheckService.CheckHealthAsync(
                    registration => registration.Tags.Contains(HealthCheckTags.Ready),
                    cancellationToken);

                return HealthCheckResponseWriter.ToResult(report, HealthCheckEndpointType.Readiness);
            })
            .WithName("MonitoringReadiness")
            .WithTags("Monitoring")
            .WithSummary("Readiness check")
            .WithDescription("Verifica se a API está pronta para receber tráfego.")
            .Produces<HealthCheckResponseDto>(StatusCodes.Status200OK)
            .Produces<HealthCheckResponseDto>(StatusCodes.Status503ServiceUnavailable)
            .AllowAnonymous();

        return endpoints;
    }
}
