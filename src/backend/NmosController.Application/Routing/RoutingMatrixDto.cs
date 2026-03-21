namespace NmosController.Application.Routing;

public sealed record RoutingMatrixDto(
    IReadOnlyCollection<RoutingSourceDto> Sources,
    IReadOnlyCollection<RoutingDestinationDto> Destinations,
    IReadOnlyCollection<RoutingCrosspointDto> Crosspoints,
    DateTimeOffset RefreshedAtUtc);
