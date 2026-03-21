namespace NmosController.Application.Routing;

public sealed record RoutingCrosspointDto(
    string DestinationId,
    string SourceId,
    string Layer,
    bool IsCompatible,
    bool IsActive,
    bool IsBreakaway);
