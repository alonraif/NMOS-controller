using NmosController.Infrastructure.Nmos.Clients;
using NmosController.Infrastructure.Nmos.Dtos.Is04;

namespace NmosController.Infrastructure.Tests;

public sealed class NmosQueryApiClientTests
{
    [Fact]
    public void ResolveConnectionBaseUrl_WhenSrCtrlControlPresent_ReturnsControlHost()
    {
        var device = CreateDevice(
            ("urn:x-nmos:control:sr-ctrl/v1.1", "http://172.16.32.72:4003/"),
            ("urn:x-nmos:control:events/v1.0", "http://172.16.32.72:4010/"));

        var result = NmosQueryApiClient.ResolveConnectionBaseUrl(device);

        Assert.Equal("http://172.16.32.72:4003", result);
    }

    [Fact]
    public void ResolveConnectionBaseUrl_WhenCmCtrlControlPresent_ReturnsControlHost()
    {
        var device = CreateDevice(
            ("urn:x-nmos:control:cm-ctrl/v1.1", "http://172.16.32.72:4003/"));

        var result = NmosQueryApiClient.ResolveConnectionBaseUrl(device);

        Assert.Equal("http://172.16.32.72:4003", result);
    }

    [Fact]
    public void ResolveConnectionBaseUrl_WhenConnectionHrefPresent_ReturnsControlHost()
    {
        var device = CreateDevice(
            ("urn:x-nmos:control:events/v1.0", "http://172.16.32.72:4003/x-nmos/connection/v1.1/"));

        var result = NmosQueryApiClient.ResolveConnectionBaseUrl(device);

        Assert.Equal("http://172.16.32.72:4003", result);
    }

    [Fact]
    public void ResolveConnectionBaseUrl_WhenNoMatchingControl_ReturnsNull()
    {
        var device = CreateDevice(
            ("urn:x-nmos:control:events/v1.0", "http://172.16.32.72:4010/"));

        var result = NmosQueryApiClient.ResolveConnectionBaseUrl(device);

        Assert.Null(result);
    }

    private static NmosDeviceResourceDto CreateDevice(params (string Type, string Href)[] controls) =>
        new()
        {
            Id = "device-1",
            NodeId = "node-1",
            Label = "Test Device",
            DeviceType = "urn:x-nmos:device:generic",
            Controls = controls
                .Select(control => new NmosDeviceControlDto
                {
                    Type = control.Type,
                    Href = control.Href
                })
                .ToList()
        };
}
