using Amazon.SQS;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Infrastructure.Messaging;
using FieldMonitoring.Infrastructure.Persistence;
using FieldMonitoring.Infrastructure.Persistence.SqlServer;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using FieldMonitoring.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Infrastructure;

/// <summary>
/// Métodos de extensão para registrar serviços da camada Infrastructure.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registra serviços da camada Infrastructure (adapters, DbContext, messaging).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FieldMonitoringDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("SqlServer");
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                // Use in-memory database for development/testing
                options.UseInMemoryDatabase("FieldMonitoring");
            }
        });

        // Repositories (DDD Aggregates)
        services.AddScoped<IFieldRepository, FieldRepository>();

        // SQL Server adapters
        services.AddScoped<IAlertStore, SqlServerAlertAdapter>();
        services.AddScoped<IIdempotencyStore, SqlServerIdempotencyAdapter>();

        // Time-series adapter (in-memory for MVP)
        services.AddSingleton<ITimeSeriesReadingsStore, InMemoryTimeSeriesAdapter>();

        return services;
    }

    /// <summary>
    /// Registra serviços de mensageria SQS.
    /// </summary>
    public static IServiceCollection AddSqsMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure SQS options
        IConfigurationSection sqsSection = configuration.GetSection(SqsOptions.SectionName);
        services.Configure<SqsOptions>(sqsSection);

        // Get region from configuration
        SqsOptions sqsOptions = new SqsOptions();
        sqsSection.Bind(sqsOptions);
        var region = sqsOptions.Region ?? "us-east-1";

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            AmazonSQSConfig config = new AmazonSQSConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            };
            return new AmazonSQSClient(config);
        });

        // Register the background consumer service
        services.AddHostedService<SqsConsumerService>();

        return services;
    }
}
