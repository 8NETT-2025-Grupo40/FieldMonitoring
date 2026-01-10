using System.Net.Http.Json;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Alerts;

/// <summary>
/// Testes de integração para alertas de ar seco (DryAir).
/// Regra: umidade do ar < 20% por 6 horas.
/// Limite strict: 20% exato NÃO gera alerta.
/// </summary>
public class DryAirAlertIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public DryAirAlertIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_CreateAlert_WhenAirHumidityBelow20For7Hours()
    {
        // Arrange - Leituras abaixo de 20% por >6h
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-dryair-1", "farm-1")
                .WithAirHumidity(15.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-7))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-dryair-1", "farm-1")
                .WithAirHumidity(18.0)
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
        var response = await _client.GetAsync("/api/fields/field-dryair-1/alerts");
        response.EnsureSuccessStatusCode();

        // Assert
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
        alerts!.Should().HaveCount(1);
        alerts[0].AlertType.ToString().Should().Be("DryAir");
        alerts[0].Status.ToString().Should().Be("Active");
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirHumidityRecovers()
    {
        // Arrange - Criar alerta de ar seco
        var dryMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-dryair-2", "farm-1")
                .WithAirHumidity(15.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-8))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-dryair-2", "farm-1")
                .WithAirHumidity(18.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in dryMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Verificar que alerta foi criado
        var checkResponse = await _client.GetAsync("/api/fields/field-dryair-2/alerts");
        var checkAlerts = await checkResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        checkAlerts.Should().HaveCount(1, "alerta deveria ter sido criado");

        // Act - Recuperar umidade (acima ou igual a 20%)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-dryair-2", "farm-1")
            .WithAirHumidity(35.0)
            .WithTimestamp(DateTime.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/api/fields/field-dryair-2/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();

        var historyResponse = await _client.GetAsync("/api/fields/field-dryair-2/alerts/history");
        var historyAlerts = await historyResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        historyAlerts.Should().NotBeNull();
        historyAlerts!.Should().HaveCount(1);
        historyAlerts[0].Status.ToString().Should().Be("Resolved");
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenAirHumidityExactly20Percent()
    {
        // Arrange - Umidade exatamente no threshold (20%) - limite strict
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-dryair-3", "farm-1")
                .WithAirHumidity(20.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-7))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-dryair-3", "farm-1")
                .WithAirHumidity(20.0)
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
        var response = await _client.GetAsync("/api/fields/field-dryair-3/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (20% exato é condição normal)
        alerts.Should().NotBeNull();
        alerts!.Where(a => a.AlertType.ToString() == "DryAir").Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ResolveAlert_WhenAirHumidityExactly20Percent()
    {
        // Arrange - Criar alerta de ar seco
        var dryMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-dryair-4", "farm-1")
                .WithAirHumidity(15.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-8))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-dryair-4", "farm-1")
                .WithAirHumidity(18.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-1))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in dryMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Umidade volta para exatamente 20% (condição normal)
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField("field-dryair-4", "farm-1")
            .WithAirHumidity(20.0)
            .WithTimestamp(DateTime.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert - Alerta deve estar resolvido
        var activeResponse = await _client.GetAsync("/api/fields/field-dryair-4/alerts");
        var activeAlerts = await activeResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        activeAlerts.Should().NotBeNull();
        activeAlerts!.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_NotCreateAlert_WhenBelow20PercentForLessThan6Hours()
    {
        // Arrange - Apenas 3 horas abaixo de 20%
        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField("field-dryair-5", "farm-1")
                .WithAirHumidity(15.0)
                .WithTimestamp(DateTime.UtcNow.AddHours(-3))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-dryair-5", "farm-1")
                .WithAirHumidity(18.0)
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
        var response = await _client.GetAsync("/api/fields/field-dryair-5/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta (janela de 6h não foi atingida)
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
                .ForField("field-dryair-6", "farm-1")
                .WithSoilMoisture(50.0) // Apenas umidade do solo
                .WithTimestamp(DateTime.UtcNow.AddHours(-7))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField("field-dryair-6", "farm-1")
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
        var response = await _client.GetAsync("/api/fields/field-dryair-6/alerts");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();

        // Assert - Não deve criar alerta de ar seco quando não há dados de umidade do ar
        alerts.Should().NotBeNull();
        alerts!.Where(a => a.AlertType.ToString() == "DryAir").Should().BeEmpty();
    }
}
