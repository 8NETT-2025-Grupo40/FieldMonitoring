using System.Net;
using System.Net.Http.Json;
using FieldMonitoring.Api.Tests;
using FieldMonitoring.Application.Alerts;
using FieldMonitoring.Application.Fields;

namespace FieldMonitoring.Api.Tests.Controllers;

public class FarmsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FarmsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOverview_ShouldReturnEmptyListForNewFarm()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/farms/new-farm/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        FarmOverviewDto? content = await response.Content.ReadFromJsonAsync<FarmOverviewDto>();
        content.Should().NotBeNull();
        content!.FarmId.Should().Be("new-farm");
        content.TotalFields.Should().Be(0);
        content.Fields.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAlerts_ShouldReturnEmptyListForNewFarm()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/farms/new-farm/alerts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<AlertDto>? content = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }
}
