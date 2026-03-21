namespace NmosController.Application.Routing;

public sealed record RoutingDestinationRouteDto(
    string Layer,
    bool IsSupported,
    string? ReceiverId,
    string? ActiveSourceId,
    string? ActiveSourceLabel,
    string? ActiveSenderId,
    string? StagedSourceId,
    string? StagedSourceLabel,
    string? StagedSenderId,
    string RedundancyStatus,
    bool IsBreakaway);
