namespace NmosController.Application.Topology;

public sealed record RoutingDestinationSnapshotDto(
    string Id,
    string Label,
    string NodeId,
    string DeviceId,
    string? VideoReceiverId,
    string? AudioReceiverId,
    string? AncillaryReceiverId,
    IReadOnlyCollection<string> Tags);
