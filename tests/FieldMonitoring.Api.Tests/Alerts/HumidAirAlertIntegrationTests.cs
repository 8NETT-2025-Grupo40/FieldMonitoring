using System.Net.Http.Json;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Alerts;

/// <summary>
/// Testes de integração para alertas de ar úmido (HumidAir).
/// Regra: umidade do ar > 90% por 12 horas.
/// Limite strict: 90% exato NÃO gera alerta.
/// </summary>
public class HumidAirAlertIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public HumidAirAlertIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_CreateAlert_WhenAirHumidityAbove90For13Hours()
    {
        // Arrange - Leituras acima de 90% por >12h
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-1", "farm-1")
                .WithAirHumidity(92.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-13))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-1", "farm-1")
                .WithAirHumidity(95.0)
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
        var response = await _client.GetAsync("/monitoring/fields/field-humid-1/alerts");
        response.EnsureSuccessStatusCode();

        // Assert
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("HumidAir");
        alerts[0].Status.ToString().Should().Be("Active");
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirHumidityRecovers()
    {
        // Arrange - Criar alerta de ar úmido
        var humidMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-2", "farm-1")
                .WithAirHumidity(92.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-14))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-2", "farm-1")
                .WithAirHumidity(95.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in humidMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Verificar que alerta foi criado
        var checkResponse = await _client.GetAsync("/monitoring/fields/field-humid-2/alerts");
        var checkAlerts = await checkResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        checkAlerts.Should().HaveCount(1, "alerta deveria ter sido criado");

        // Act - Recuperar umidade (abaixo ou igual a 90%)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-humid-2", "farm-1")
            .WithAirHumidity(70.0)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/monitoring/fields/field-humid-2/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();

        var historyResponse = await _client.GetAsync("/monitoring/fields/field-humid-2/alerts/history");
        var historyAlerts = await historyResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        historyAlerts.Should().NotBeNull();
        historyAlerts!.Should().HaveCount(1);
        historyAlerts[0].Status.ToString().Should().Be("Resolved");
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAirHumidityExactly90Percent()
    {
        // Arrange - Umidade exatamente no threshold (90%) - limite strict
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-3", "farm-1")
                .WithAirHumidity(90.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-13))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-3", "farm-1")
                .WithAirHumidity(90.0)
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
        var response = await _client.GetAsync("/monitoring/fields/field-humid-3/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (90% exato é condição normal)
        alerts.Should().NotBeNull();
        alerts!.Where(a => a.AlertType.ToString() == "HumidAir").Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirHumidityExactly90Percent()
    {
        // Arrange - Criar alerta de ar úmido
        var humidMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-4", "farm-1")
                .WithAirHumidity(92.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-14))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-4", "farm-1")
                .WithAirHumidity(95.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in humidMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Umidade volta para exatamente 90% (condição normal)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-humid-4", "farm-1")
            .WithAirHumidity(90.0)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/monitoring/fields/field-humid-4/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAbove90PercentForLessThan12Hours()
    {
        // Arrange - Apenas 6 horas acima de 90%
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-5", "farm-1")
                .WithAirHumidity(92.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-6))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-5", "farm-1")
                .WithAirHumidity(95.0)
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
        var response = await _client.GetAsync("/monitoring/fields/field-humid-5/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (janela de 12h não foi atingida)
        alerts.Should().NotBeNull();
        alerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAirHumidityIsNull()
    {
        // Arrange - Leituras sem umidade do ar
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-6", "farm-1")
                .WithSoilMoisture(50.0) // Apenas umidade do solo
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-13))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-6", "farm-1")
                .WithSoilMoisture(50.0)
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
        var response = await _client.GetAsync("/monitoring/fields/field-humid-6/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta de ar úmido quando não há dados de umidade do ar
        alerts.Should().NotBeNull();
        alerts!.Where(a => a.AlertType.ToString() == "HumidAir").Should().BeEmpty();
    }

    [Fact]
    public async Task Should_CreateAlert_When100PercentHumidityFor13Hours()
    {
        // Arrange - Umidade máxima (100%)
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-humid-7", "farm-1")
                .WithAirHumidity(100.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-13))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-humid-7", "farm-1")
                .WithAirHumidity(100.0)
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
        var response = await _client.GetAsync("/monitoring/fields/field-humid-7/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Deve criar alerta de ar úmido
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("HumidAir");
    }
}
