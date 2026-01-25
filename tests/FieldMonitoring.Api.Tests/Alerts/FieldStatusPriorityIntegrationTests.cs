using System.Net.Http.Json;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Alerts;

/// <summary>
/// Testes de integração para validar a prioridade de status do talhão.
/// Prioridade: Frost > Heat > Dryness > DryAir > HumidAir > Normal
/// Quando múltiplos alertas estão ativos, o status deve refletir o mais crítico.
/// </summary>
public class FieldStatusPriorityIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public FieldStatusPriorityIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_ShowFrostStatus_WhenFrostAndHeatAlertsActive()
    {
        // Arrange - Criar condições para ambos alertas: Frost e Heat
        // Frost: <2°C por 2h, Heat: >40°C não vai ser possível ao mesmo tempo
        // Simula situação onde frost foi disparado e heat estava ativo antes
        var fieldId = "field-priority-1";

        // Primeiro: criar alerta de calor
        var heatMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithAirTemperature(42.0)
                .WithSoilMoisture(50.0) // Normal
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-10))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithAirTemperature(43.0)
                .WithSoilMoisture(50.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-5))
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in heatMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Verificar que Heat foi ativado
        var heatCheck = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var fieldAfterHeat = await heatCheck.Content.ReadFromJsonAsync<FieldDetailDto>();
        fieldAfterHeat!.Status.ToString().Should().Be("HeatAlert");

        // Agora: criar alerta de geada (temperatura cai drasticamente)
        var frostMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithAirTemperature(1.0)
                .WithSoilMoisture(50.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-3))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithAirTemperature(0.5)
                .WithSoilMoisture(50.0)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in frostMessages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();

        // Assert - Frost tem prioridade sobre Heat
        field.Should().NotBeNull();
        field!.Status.ToString().Should().Be("FrostAlert");
    }

    [Fact]
    public async Task Should_ShowHeatStatus_WhenHeatAndDrynessAlertsActive()
    {
        // Arrange - Criar condições para ambos: Heat e Dryness
        var fieldId = "field-priority-2";

        var messages = new[]
        {
            // Primeira leitura: inicia tracking de ambos
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithAirTemperature(42.0) // Acima de 40°C
                .WithSoilMoisture(25.0)   // Abaixo de 30%
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-25))
                .Build(),
            // Segunda leitura: mantém condições
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithAirTemperature(43.0)
                .WithSoilMoisture(22.0)
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
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();

        // Assert - Heat tem prioridade sobre Dryness
        field.Should().NotBeNull();
        field!.Status.ToString().Should().Be("HeatAlert");
    }

    [Fact]
    public async Task Should_ShowDrynessStatus_WhenDrynessAndDryAirAlertsActive()
    {
        // Arrange - Criar condições para ambos: Dryness e DryAir
        var fieldId = "field-priority-3";

        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(25.0)  // Abaixo de 30%
                .WithAirHumidity(15.0)   // Abaixo de 20%
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-25))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(22.0)
                .WithAirHumidity(18.0)
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
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();

        // Assert - Dryness tem prioridade sobre DryAir
        field.Should().NotBeNull();
        field!.Status.ToString().Should().Be("DryAlert");
    }

    [Fact]
    public async Task Should_ShowDryAirStatus_WhenOnlyDryAirAlertActive()
    {
        // Arrange - Apenas ar seco abaixo do threshold
        var fieldId = "field-priority-4";

        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(50.0)    // Normal
                .WithAirTemperature(25.0)  // Normal
                .WithAirHumidity(15.0)     // Abaixo de 20%
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-7))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(50.0)
                .WithAirTemperature(25.0)
                .WithAirHumidity(18.0)
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
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();

        // Assert - DryAir é o único alerta ativo
        field.Should().NotBeNull();
        field!.Status.ToString().Should().Be("DryAirAlert");
    }

    [Fact]
    public async Task Should_ShowNormalStatus_WhenNoAlertsActive()
    {
        // Arrange - Condições normais
        var fieldId = "field-priority-5";

        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(50.0)    // Normal (>30%)
                .WithAirTemperature(25.0)  // Normal (<40°C e >2°C)
                .WithAirHumidity(50.0)     // Normal (>20% e <90%)
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
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();

        // Assert - Status deve ser Normal
        field.Should().NotBeNull();
        field!.Status.ToString().Should().Be("Normal");
    }

    [Fact]
    public async Task Should_TransitionFromAlertToNormal_WhenConditionsRecover()
    {
        // Arrange - Criar alerta de seca
        var fieldId = "field-priority-6";

        var dryMessages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(25.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-25))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(22.0)
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
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
        var alertCheck = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var fieldWithAlert = await alertCheck.Content.ReadFromJsonAsync<FieldDetailDto>();
        fieldWithAlert!.Status.ToString().Should().Be("DryAlert");

        // Act - Recuperar condições
        var recoveryMessage = new TelemetryMessageBuilder()
            .ForField(fieldId, "farm-1")
            .WithSoilMoisture(40.0) // Normal
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryMessage);
        }

        // Assert
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();
        field!.Status.ToString().Should().Be("Normal");
    }

    [Fact]
    public async Task Should_ShowHumidAirStatus_WhenOnlyHumidAirAlertActive()
    {
        // Arrange - Apenas ar úmido acima do threshold
        var fieldId = "field-priority-7";

        var messages = new[]
        {
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(50.0)    // Normal
                .WithAirTemperature(25.0)  // Normal
                .WithAirHumidity(92.0)     // Acima de 90%
                .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-13))
                .Build(),
            new TelemetryMessageBuilder()
                .ForField(fieldId, "farm-1")
                .WithSoilMoisture(50.0)
                .WithAirTemperature(25.0)
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
        var response = await _client.GetAsync($"/monitoring/fields/{fieldId}");
        var field = await response.Content.ReadFromJsonAsync<FieldDetailDto>();

        // Assert - HumidAir é o único alerta ativo
        field.Should().NotBeNull();
        field!.Status.ToString().Should().Be("HumidAirAlert");
    }
}
