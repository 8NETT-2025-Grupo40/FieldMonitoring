using System.Net;
using System.Net.Http.Json;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Fields;

/// <summary>
/// Testes de integração para campos.
/// Valida transições de status e consultas.
/// </summary>
public class FieldIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public FieldIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_TransitionStatus_WhenConditionsChange()
    {
        // Arrange - Estado inicial Normal
        var normalReading = new TelemetryMessageBuilder()
            .ForField("field-3", "farm-1")
            .WithSoilMoisture(40.0)
            .WithTimestamp(DateTime.UtcNow.AddHours(-30))
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(normalReading);
        }

        var response1 = await _client.GetAsync("/api/fields/field-3");
        var field1 = await response1.Content.ReadFromJsonAsync<FieldDetailDto>();
        field1!.Status.ToString().Should().Be("Normal");

        // Act 1 - Transição para DryAlert
        var dryReading = new TelemetryMessageBuilder()
            .ForField("field-3", "farm-1")
            .WithSoilMoisture(20.0)
            .WithTimestamp(DateTime.UtcNow.AddHours(-1))
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(dryReading);
        }

        var response2 = await _client.GetAsync("/api/fields/field-3");
        var field2 = await response2.Content.ReadFromJsonAsync<FieldDetailDto>();
        field2!.Status.ToString().Should().Be("DryAlert");
        field2.ActiveAlerts.Should().HaveCount(1);

        // Act 2 - Transição de volta para Normal
        var recoveryReading = new TelemetryMessageBuilder()
            .ForField("field-3", "farm-1")
            .WithSoilMoisture(45.0)
            .WithTimestamp(DateTime.UtcNow)
            .Build();

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            await useCase.ExecuteAsync(recoveryReading);
        }

        var response3 = await _client.GetAsync("/api/fields/field-3");
        var field3 = await response3.Content.ReadFromJsonAsync<FieldDetailDto>();
        field3!.Status.ToString().Should().Be("Normal");
        field3.ActiveAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Return404_WhenFieldDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/fields/non-existent-field-xyz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
