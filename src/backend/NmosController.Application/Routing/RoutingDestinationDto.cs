namespace NmosController.Application.Routing;

public sealed record RoutingDestinationDto(
    string Id,
    string Label,
    string NodeId,
    string DeviceId,
    IReadOnlyCollection<RoutingDestinationRouteDto> Routes,
    IReadOnlyCollection<string> Tags);
