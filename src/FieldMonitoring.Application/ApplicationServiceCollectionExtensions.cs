using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Application;

/// <summary>
/// Métodos de extensão para registrar serviços da camada Application.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registra serviços da camada Application (use cases, queries, services).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IRuleSetProvider, DefaultRuleSetProvider>();

        // Use Cases
        services.AddScoped<ProcessTelemetryReadingUseCase>();

        // Queries
        services.AddScoped<GetFarmOverviewQuery>();
        services.AddScoped<GetFieldDetailQuery>();
        services.AddScoped<GetFieldHistoryQuery>();
        services.AddScoped<GetActiveAlertsQuery>();
        services.AddScoped<GetAlertHistoryQuery>();
        services.AddScoped<GetAlertByIdQuery>();

        return services;
    }
}
