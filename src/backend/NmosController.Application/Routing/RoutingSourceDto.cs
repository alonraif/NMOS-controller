namespace NmosController.Application.Routing;

public sealed record RoutingSourceDto(
    string Id,
    string Label,
    string Layer,
    string? PrimarySenderId,
    string? SecondarySenderId,
    string RedundancyStatus,
    bool IsAvailable,
    string Transport,
    string Format,
    string NodeId,
    string DeviceId);
