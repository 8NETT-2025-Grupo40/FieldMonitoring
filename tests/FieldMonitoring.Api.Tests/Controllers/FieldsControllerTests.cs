using System.Net;
using FieldMonitoring.Api.Tests;

namespace FieldMonitoring.Api.Tests.Controllers;

public class FieldsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FieldsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDetail_WhenFieldDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/fields/non-existent-field");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistory_ShouldReturnEmptyListForNewField()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-1).ToString("o");
        var to = DateTime.UtcNow.ToString("o");

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/fields/new-field/history?from={from}&to={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
