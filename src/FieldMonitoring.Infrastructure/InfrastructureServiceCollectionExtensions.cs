using Amazon.SQS;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using FieldMonitoring.Infrastructure.Messaging;
using FieldMonitoring.Infrastructure.Persistence;
using FieldMonitoring.Infrastructure.Persistence.SqlServer;
using FieldMonitoring.Infrastructure.Persistence.TimeSeries;
using FieldMonitoring.Infrastructure.Repositories;
using InfluxDB.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Testes de integracao substituem o DbContext via ConfigureTestServices
        string? connectionString = configuration.GetConnectionString("SqlServer");

        services.AddDbContext<FieldMonitoringDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IFieldRepository, FieldRepository>();
        services.AddScoped<IAlertStore, SqlServerAlertAdapter>();
        services.AddScoped<IIdempotencyStore, SqlServerIdempotencyAdapter>();

        InfluxDbOptions influxOptions = InfluxDbOptions.Load(configuration);
        services.AddSingleton(influxOptions);

        if (influxOptions.Enabled)
        {
            if (!influxOptions.IsConfigured())
            {
                throw new InvalidOperationException("InfluxDB habilitado, mas com configuração incompleta.");
            }

            services.AddSingleton<IInfluxDBClient>(_ => new InfluxDBClient(influxOptions.Url!, influxOptions.Token!));
            services.AddSingleton<IInfluxBucketProbe, InfluxBucketProbe>();
            services.AddSingleton<ITimeSeriesReadingsStore, InfluxTimeSeriesAdapter>();
            services.AddSingleton<IAlertEventsStore, InfluxAlertEventsAdapter>();
        }
        else
        {
            services.AddSingleton<ITimeSeriesReadingsStore, InMemoryTimeSeriesAdapter>();
            services.AddSingleton<IAlertEventsStore, NoOpAlertEventsAdapter>();
        }

        return services;
    }

    public static IServiceCollection AddSqsMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection sqsSection = configuration.GetSection(SqsOptions.SectionName);
        services.Configure<SqsOptions>(sqsSection);

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

        services.AddHostedService<SqsConsumerService>();

        return services;
    }
}
