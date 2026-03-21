namespace NmosController.Application.Topology;

public sealed record NmosNodeDto(
    string Id,
    string Label,
    string? Hostname,
    string? Description,
    IReadOnlyCollection<string> ApiVersions,
    IReadOnlyCollection<string> Interfaces,
    DateTimeOffset LastSeenAtUtc);
