using System.Net.Http.Json;
using FieldMonitoring.Application.Fields;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Farms;

/// <summary>
/// Testes de integração para fazendas.
/// Valida agregação de múltiplos fields e overview.
/// </summary>
public class FarmIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public FarmIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task Should_AggregateMultipleFields_WhenQueryingFarmOverview()
    {
        // Arrange - Múltiplos fields na mesma farm
        var messages = new[]
        {
            // Field 1 - Normal
            new TelemetryMessageBuilder().ForField("field-A", "farm-2").WithSoilMoisture(45.0).WithTimestamp(DateTimeOffset.UtcNow).Build(),
            
            // Field 2 - Com alerta
            new TelemetryMessageBuilder().ForField("field-B", "farm-2").WithSoilMoisture(20.0).WithTimestamp(DateTimeOffset.UtcNow.AddHours(-30)).Build(),
            new TelemetryMessageBuilder().ForField("field-B", "farm-2").WithSoilMoisture(18.0).WithTimestamp(DateTimeOffset.UtcNow).Build(),
            
            // Field 3 - Normal
            new TelemetryMessageBuilder().ForField("field-C", "farm-2").WithSoilMoisture(50.0).WithTimestamp(DateTimeOffset.UtcNow).Build()
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
        var response = await _client.GetAsync("/api/farms/farm-2/overview");
        response.EnsureSuccessStatusCode();

        // Assert
        var overview = await response.Content.ReadFromJsonAsync<FarmOverviewDto>();
        overview.Should().NotBeNull();
        overview!.FarmId.Should().Be("farm-2");
        overview.Fields.Should().HaveCount(3);

        // Verificar agregação
        List<FieldOverviewDto> normalFields = overview.Fields.Where(f => f.Status.ToString() == "Normal").ToList();
        List<FieldOverviewDto> alertFields = overview.Fields.Where(f => f.Status.ToString() == "DryAlert").ToList();
        
        normalFields.Should().HaveCount(2);
        alertFields.Should().HaveCount(1);
        alertFields[0].FieldId.Should().Be("field-B");
    }
}
