using System.Net.Http.Json;
using FieldMonitoring.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace FieldMonitoring.Api.Tests.Telemetry;

public class QueryEncodingTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public QueryEncodingTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _client = _fixture.CreateClient();
    }

    [Fact]
    public async Task HistoryEndpoint_AcceptsUnencodedPlusInOffset()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var msgs = new[]
        {
            new TelemetryMessageBuilder().ForField("field-QE-1", "farm-1").WithSoilMoisture(10).WithTimestamp(now.AddHours(-2)).Build(),
            new TelemetryMessageBuilder().ForField("field-QE-1", "farm-1").WithSoilMoisture(20).WithTimestamp(now.AddHours(-1)).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var m in msgs) await useCase.ExecuteAsync(m);
        }

        var from = now.AddHours(-3);
        var to = now;

        // Act - unencoded (may contain '+')
        var response = await _client.GetAsync($"/api/fields/field-QE-1/history?from={from:O}&to={to:O}");
        response.EnsureSuccessStatusCode();

        var readings = await response.Content.ReadFromJsonAsync<List<ReadingDto>>();
        readings.Should().NotBeNull();
        readings!.Should().HaveCount(2);
    }

    [Fact]
    public async Task HistoryEndpoint_AcceptsUriEncodedPlusInOffset()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var msgs = new[]
        {
            new TelemetryMessageBuilder().ForField("field-QE-2", "farm-1").WithSoilMoisture(11).WithTimestamp(now.AddHours(-2)).Build(),
            new TelemetryMessageBuilder().ForField("field-QE-2", "farm-1").WithSoilMoisture(21).WithTimestamp(now.AddHours(-1)).Build()
        };

        using (var scope = _fixture.Services.CreateScope())
        {
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessTelemetryReadingUseCase>();
            foreach (var m in msgs) await useCase.ExecuteAsync(m);
        }

        var from = now.AddHours(-3).ToString("o");
        var to = now.ToString("o");
        var ef = Uri.EscapeDataString(from);
        var et = Uri.EscapeDataString(to);

        // Act - encoded
        var response = await _client.GetAsync($"/api/fields/field-QE-2/history?from={ef}&to={et}");
        response.EnsureSuccessStatusCode();

        var readings = await response.Content.ReadFromJsonAsync<List<ReadingDto>>();
        readings.Should().NotBeNull();
        readings!.Should().HaveCount(2);
    }
}
