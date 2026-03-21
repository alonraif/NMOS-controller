namespace NmosController.Application.Topology;

public sealed record TopologyRouteEdgeDto(
    string Id,
    string Source,
    string Target,
    string State,
    string Path,
    string Layer,
    string? RedundancyGroup,
    bool IsHealthy,
    IReadOnlyDictionary<string, string> Metadata);
