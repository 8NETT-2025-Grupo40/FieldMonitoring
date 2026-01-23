using System.Net.Http.Json;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Alerts;

/// <summary>
/// Testes de integração para alertas.
/// Valida criação, resolução e consultas de alertas.
/// </summary>
public class AlertIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public AlertIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_CreateAlert_WhenSoilMoistureBelowThresholdFor25Hours()
    {
        // Arrange - Leituras abaixo do threshold por >24h
        var messages = new[]
        {
            new TelemetryMessageBuilder().ForField("field-A1", "farm-1").WithSoilMoisture(25.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-30)).Build(),
            new TelemetryMessageBuilder().ForField("field-A1", "farm-1").WithSoilMoisture(20.0).WithTimestamp(DateTimeOffset.UtcNow).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in messages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Consultar alertas ativos
        var response = await _client.GetAsync("/api/fields/field-A1/alerts");
        response.EnsureSuccessStatusCode();

        // Assert
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("Dryness");
        alerts[0].Status.ToString().Should().Be("Active");
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenSoilMoistureRecovers()
    {
        // Arrange - Criar alerta com seca
        var dryMessages = new[]
        {
            new TelemetryMessageBuilder().ForField("field-A2", "farm-1").WithSoilMoisture(25.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-30)).Build(),
            new TelemetryMessageBuilder().ForField("field-A2", "farm-1").WithSoilMoisture(20.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1)).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in dryMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Recuperar umidade
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-A2", "farm-1")
            .WithSoilMoisture(40.0)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/api/fields/field-A2/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();

        var historyResponse = await _client.GetAsync("/api/fields/field-A2/alerts/history");
        var historyAlerts = await historyResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        historyAlerts.Should().NotBeNull();
        historyAlerts!.Should().HaveCount(1);
        historyAlerts[0].Status.ToString().Should().Be("Resolved");
        historyAlerts[0].ResolvedAt.Should().NotBeNull();
    }


    [Fact]
    public async Task Should_NotCreateAlert_WhenBelowThresholdForLessThan24Hours()
    {
        // Arrange - Apenas 12 horas abaixo do threshold
        var messages = new[]
        {
            new TelemetryMessageBuilder().ForField("field-A6", "farm-1").WithSoilMoisture(25.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-12)).Build(),
            new TelemetryMessageBuilder().ForField("field-A6", "farm-1").WithSoilMoisture(20.0).WithTimestamp(DateTimeOffset.UtcNow).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in messages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act
        var response = await _client.GetAsync("/api/fields/field-A6/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta
        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_PreventDuplicateAlerts_WhenMultipleDryReadings()
    {
        // Arrange - Múltiplas leituras secas
        var messages = new[]
        {
            new TelemetryMessageBuilder().ForField("field-A7", "farm-1").WithSoilMoisture(25.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-30)).Build(),
            new TelemetryMessageBuilder().ForField("field-A7", "farm-1").WithSoilMoisture(20.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-10)).Build(),
            new TelemetryMessageBuilder().ForField("field-A7", "farm-1").WithSoilMoisture(22.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-5)).Build(),
            new TelemetryMessageBuilder().ForField("field-A7", "farm-1").WithSoilMoisture(18.0).WithTimestamp(DateTimeOffset.UtcNow).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in messages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act
        var response = await _client.GetAsync("/api/fields/field-A7/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Deve ter apenas 1 alerta
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
    }
}
