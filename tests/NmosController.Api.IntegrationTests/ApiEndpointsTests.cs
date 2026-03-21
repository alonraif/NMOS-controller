using System.Net;
using System.Net.Http.Json;
using NmosController.Contracts.Requests;

namespace NmosController.Api.IntegrationTests;

public sealed class ApiEndpointsTests : IClassFixture<ControllerApiFactory>
{
    private readonly HttpClient _client;

    public ApiEndpointsTests(ControllerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTopology_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/topology");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRoutingMatrix_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/routing/matrix");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidateRoute_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/routing/validate",
            new RouteValidationRequest
            {
                SenderId = "sender-a",
                ReceiverId = "receiver-a"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConnectRouting_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/routing/connect",
            new RoutingConnectRequest
            {
                DestinationId = "receiver-a",
                RequestedBy = "tester",
                AudioSourceId = "source-a"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DisconnectRouting_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/routing/disconnect",
            new RoutingDisconnectRequest
            {
                DestinationId = "receiver-a",
                RequestedBy = "tester",
                DisconnectAudio = true
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
