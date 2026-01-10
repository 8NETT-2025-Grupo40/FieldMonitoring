using System.Net.Http.Json;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Alerts;

/// <summary>
/// Testes de integração para alertas de calor extremo (ExtremeHeat).
/// Regra: temperatura do ar > 40°C por 4 horas.
/// Limite strict: 40°C exato NÃO gera alerta.
/// </summary>
public class ExtremeHeatAlertIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public ExtremeHeatAlertIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_CreateAlert_WhenAirTemperatureAbove40CFor5Hours()
    {
        // Arrange - Leituras acima de 40°C por >4h
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-heat-1", "farm-1")
                .WithAirTemperature(42.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-5))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-heat-1", "farm-1")
                .WithAirTemperature(43.0)
                .WithTimestamp(DateTime.UtcNow)
                .Build()
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
        var response = await _client.GetAsync("/api/fields/field-heat-1/alerts");
        response.EnsureSuccessStatusCode();

        // Assert
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("ExtremeHeat");
        alerts[0].Status.ToString().Should().Be("Active");
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirTemperatureRecovers()
    {
        // Arrange - Criar alerta de calor extremo
        var hotMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-heat-2", "farm-1")
                .WithAirTemperature(42.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-6))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-heat-2", "farm-1")
                .WithAirTemperature(43.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in hotMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Verificar que alerta foi criado
        var checkResponse = await _client.GetAsync("/api/fields/field-heat-2/alerts");
        var checkAlerts = await checkResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        checkAlerts.Should().HaveCount(1, "alerta deveria ter sido criado");

        // Act - Recuperar temperatura (abaixo ou igual a 40°C)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-heat-2", "farm-1")
            .WithAirTemperature(38.0)
            .WithTimestamp(DateTime.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/api/fields/field-heat-2/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();

        var historyResponse = await _client.GetAsync("/api/fields/field-heat-2/alerts/history");
        var historyAlerts = await historyResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        historyAlerts.Should().NotBeNull();
        historyAlerts!.Should().HaveCount(1);
        historyAlerts[0].Status.ToString().Should().Be("Resolved");
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAirTemperatureExactly40C()
    {
        // Arrange - Temperatura exatamente no threshold (40°C) - limite strict
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-heat-3", "farm-1")
                .WithAirTemperature(40.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-5))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-heat-3", "farm-1")
                .WithAirTemperature(40.0)
                .WithTimestamp(DateTime.UtcNow)
                .Build()
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
        var response = await _client.GetAsync("/api/fields/field-heat-3/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (40°C exato é condição normal)
        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirTemperatureExactly40C()
    {
        // Arrange - Criar alerta de calor extremo
        var hotMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-heat-4", "farm-1")
                .WithAirTemperature(42.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-6))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-heat-4", "farm-1")
                .WithAirTemperature(43.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in hotMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Temperatura volta para exatamente 40°C (condição normal)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-heat-4", "farm-1")
            .WithAirTemperature(40.0)
            .WithTimestamp(DateTime.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/api/fields/field-heat-4/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAbove40CForLessThan4Hours()
    {
        // Arrange - Apenas 2 horas acima de 40°C
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-heat-5", "farm-1")
                .WithAirTemperature(42.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-2))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-heat-5", "farm-1")
                .WithAirTemperature(43.0)
                .WithTimestamp(DateTime.UtcNow)
                .Build()
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
        var response = await _client.GetAsync("/api/fields/field-heat-5/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (janela de 4h não foi atingida)
        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAirTemperatureIsNull()
    {
        // Arrange - Leituras sem temperatura do ar
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-heat-6", "farm-1")
                .WithSoilMoisture(50.0) // Apenas umidade do solo
                .WithTimestamp(DateTime.UtcNow.AddHours(-5))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-heat-6", "farm-1")
                .WithSoilMoisture(50.0)
                .WithTimestamp(DateTime.UtcNow)
                .Build()
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
        var response = await _client.GetAsync("/api/fields/field-heat-6/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta de calor quando não há dados de temperatura do ar
        alerts.Should().NotBeNull();
        alerts!.Where(a => a.AlertType.ToString() == "ExtremeHeat").Should().BeEmpty();
    }
}
