using System.Net.Http.Json;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Telemetry;

/// <summary>
/// Testes de integração para processamento de telemetria.
/// Valida recepção, persistência, histórico e agregações.
/// </summary>
public class TelemetryIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public TelemetryIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_PersistReading_WhenProcessingTelemetry()
    {
        // Arrange
        var message = new TelemetryMessageBuilder()
            .ForField("field-1", "farm-1")
            .WithSoilMoisture(45.0)
            .WithTemperature(25.5)
            .WithRain(10.2)
            .WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1))
            .Build();

        // Act - Processar telemetria
        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(message);
        }

        // Assert - Verificar via API
        var response = await _client.GetAsync("/monitoring/fields/field-1");
        response.EnsureSuccessStatusCode();

        var field = await response.Content.ReadFromJsonAsync<Application.Fields.FieldDetailDto>();
        field.Should().NotBeNull();
        field!.FieldId.Should().Be("field-1");
        field.FarmId.Should().Be("farm-1");
        field.LastSoilHumidity.Should().Be(45.0);
        field.LastSoilTemperature.Should().Be(25.5);
        field.LastRainMm.Should().Be(10.2);
    }

    [Fact]
    public async Task Should_ReturnAllReadings_WhenQueryingHistory()
    {
        // Arrange - Múltiplas leituras
        var messages = new[]
        {
            new TelemetryMessageBuilder().ForField("field-T1", "farm-1").WithSoilMoisture(40.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-3)).Build(),
            new TelemetryMessageBuilder().ForField("field-T1", "farm-1").WithSoilMoisture(42.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-2)).Build(),
            new TelemetryMessageBuilder().ForField("field-T1", "farm-1").WithSoilMoisture(44.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-1)).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in messages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Buscar histórico
        var from = DateTimeOffset.UtcNow.AddHours(-4);
        var to = DateTimeOffset.UtcNow;
        var response = await _client.GetAsync($"/monitoring/fields/field-T1/history?from={from:O}&to={to:O}");
        response.EnsureSuccessStatusCode();

        // Assert
        var readings = await response.Content.ReadFromJsonAsync<List<ReadingDto>>();
        readings.Should().NotBeNull();
        readings!.Should().HaveCount(3);
        readings.Select(r => r.SoilHumidity).Should().ContainInOrder(40.0, 42.0, 44.0);
    }

    [Fact]
    public async Task Should_BeIdempotent_WhenProcessingSameReadingTwice()
    {
        // Arrange
        var message = new TelemetryMessageBuilder()
            .ForField("field-2", "farm-1")
            .WithSoilMoisture(50.0)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        // Act - Processar 2x
        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(message);
            var result2 = await useCase.ExecuteAsync(message);

            // Assert - Segunda execução deve ser ignorada (idempotente)
            result2.ToString().Should().Contain("Skipped");
        }
    }

    [Fact]
    public async Task Should_FilterByDateRange_WhenQueryingHistory()
    {
        // Arrange - Leituras em diferentes horários
        var now = DateTimeOffset.UtcNow;
        var messages = new[]
        {
            new TelemetryMessageBuilder().ForField("field-2", "farm-1").WithSoilMoisture(30.0).WithTimestamp(now.AddHours(-5)).Build(),
            new TelemetryMessageBuilder().ForField("field-2", "farm-1").WithSoilMoisture(35.0).WithTimestamp(now.AddHours(-2)).Build(),
            new TelemetryMessageBuilder().ForField("field-2", "farm-1").WithSoilMoisture(40.0).WithTimestamp(now.AddHours(-1)).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var msg in messages)
            {
                await useCase.ExecuteAsync(msg);
            }
        }

        // Act - Filtrar apenas últimas 3 horas
        var from = now.AddHours(-3);
        var to = now;
        var response = await _client.GetAsync($"/monitoring/fields/field-2/history?from={from:O}&to={to:O}");
        response.EnsureSuccessStatusCode();

        // Assert - Deve retornar apenas 2 leituras
        var readings = await response.Content.ReadFromJsonAsync<List<ReadingDto>>();
        readings.Should().NotBeNull();
        readings!.Should().HaveCount(2);
        readings.Select(r => r.SoilHumidity).Should().ContainInOrder(35.0, 40.0);
    }

    [Fact]
    public async Task Should_PersistOutOfOrderReadingInHistory_WithoutChangingFieldOperationalState()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var newerMessage = new TelemetryMessageBuilder()
            .WithReadingId($"newer-{Guid.NewGuid():N}")
            .ForField("field-oo-1", "farm-1")
            .WithSoilMoisture(55.0)
            .WithTimestamp(now)
            .Build();

        var outOfOrderMessage = new TelemetryMessageBuilder()
            .WithReadingId($"older-{Guid.NewGuid():N}")
            .ForField("field-oo-1", "farm-1")
            .WithSoilMoisture(15.0)
            .WithTimestamp(now.AddHours(-2))
            .Build();

        ProcessingResult outOfOrderResult;
        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(newerMessage);
            outOfOrderResult = await useCase.ExecuteAsync(outOfOrderMessage);
        }

        // Assert - leitura fora de ordem é persistida no histórico, mas não altera estado operacional
        outOfOrderResult.IsSuccess.Should().BeTrue();
        outOfOrderResult.WasSkipped.Should().BeTrue();

        var fieldResponse = await _client.GetAsync("/monitoring/fields/field-oo-1");
        fieldResponse.EnsureSuccessStatusCode();

        var field = await fieldResponse.Content.ReadFromJsonAsync<Application.Fields.FieldDetailDto>();
        field.Should().NotBeNull();
        field!.LastSoilHumidity.Should().Be(55.0);
        field.LastReadingAt.Should().Be(newerMessage.Timestamp);

        var from = now.AddHours(-3);
        var to = now.AddHours(1);
        var historyResponse = await _client.GetAsync($"/monitoring/fields/field-oo-1/history?from={from:O}&to={to:O}");
        historyResponse.EnsureSuccessStatusCode();

        var readings = await historyResponse.Content.ReadFromJsonAsync<List<ReadingDto>>();
        readings.Should().NotBeNull();
        readings!.Should().HaveCount(2);
        readings.Select(r => r.SoilHumidity).Should().ContainInOrder(15.0, 55.0);
    }

}
