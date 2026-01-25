using System.Net.Http.Json;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Alerts;

/// <summary>
/// Testes de integração para alertas de geada (Frost).
/// Regra: temperatura do ar < 2°C por 2 horas.
/// Limite strict: 2°C exato NÃO gera alerta.
/// </summary>
public class FrostAlertIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public FrostAlertIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_CreateAlert_WhenAirTemperatureBelow2CFor3Hours()
    {
        // Arrange - Leituras abaixo de 2°C por >2h
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-frost-1", "farm-1")
                .WithAirTemperature(0.5)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-3))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-frost-1", "farm-1")
                .WithAirTemperature(1.0)
                .WithTimestamp(DateTimeOffset.UtcNow)
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
        var response = await _client.GetAsync("/monitoring/fields/field-frost-1/alerts");
        response.EnsureSuccessStatusCode();

        // Assert
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("Frost");
        alerts[0].Status.ToString().Should().Be("Active");
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirTemperatureRecovers()
    {
        // Arrange - Criar alerta de geada
        var coldMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-frost-2", "farm-1")
                .WithAirTemperature(0.5)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-4))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-frost-2", "farm-1")
                .WithAirTemperature(1.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in coldMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Verificar que alerta foi criado
        var checkResponse = await _client.GetAsync("/monitoring/fields/field-frost-2/alerts");
        var checkAlerts = await checkResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        checkAlerts.Should().HaveCount(1, "alerta deveria ter sido criado");

        // Act - Recuperar temperatura (acima ou igual a 2°C)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-frost-2", "farm-1")
            .WithAirTemperature(5.0)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/monitoring/fields/field-frost-2/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();

        var historyResponse = await _client.GetAsync("/monitoring/fields/field-frost-2/alerts/history");
        var historyAlerts = await historyResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        historyAlerts.Should().NotBeNull();
        historyAlerts!.Should().HaveCount(1);
        historyAlerts[0].Status.ToString().Should().Be("Resolved");
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAirTemperatureExactly2C()
    {
        // Arrange - Temperatura exatamente no threshold (2°C) - limite strict
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-frost-3", "farm-1")
                .WithAirTemperature(2.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-3))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-frost-3", "farm-1")
                .WithAirTemperature(2.0)
                .WithTimestamp(DateTimeOffset.UtcNow)
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
        var response = await _client.GetAsync("/monitoring/fields/field-frost-3/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (2°C exato é condição normal)
        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirTemperatureExactly2C()
    {
        // Arrange - Criar alerta de geada
        var coldMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-frost-4", "farm-1")
                .WithAirTemperature(0.5)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-4))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-frost-4", "farm-1")
                .WithAirTemperature(1.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in coldMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Temperatura volta para exatamente 2°C (condição normal)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-frost-4", "farm-1")
            .WithAirTemperature(2.0)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/monitoring/fields/field-frost-4/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenBelow2CForLessThan2Hours()
    {
        // Arrange - Apenas 1 hora abaixo de 2°C
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-frost-5", "farm-1")
                .WithAirTemperature(0.5)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-frost-5", "farm-1")
                .WithAirTemperature(1.0)
                .WithTimestamp(DateTimeOffset.UtcNow)
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
        var response = await _client.GetAsync("/monitoring/fields/field-frost-5/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (janela de 2h não foi atingida)
        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_CreateAlert_WhenNegativeTemperatureFor3Hours()
    {
        // Arrange - Temperaturas negativas (congelamento)
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-frost-6", "farm-1")
                .WithAirTemperature(-2.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-3))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-frost-6", "farm-1")
                .WithAirTemperature(-1.5)
                .WithTimestamp(DateTimeOffset.UtcNow)
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
        var response = await _client.GetAsync("/monitoring/fields/field-frost-6/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Deve criar alerta de geada
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("Frost");
    }
}
