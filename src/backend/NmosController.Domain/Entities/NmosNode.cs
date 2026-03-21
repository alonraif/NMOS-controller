namespace NmosController.Domain.Entities;

public sealed class NmosNode
{
    public string Id { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Hostname { get; init; }
    public string? Description { get; init; }
    public IReadOnlyCollection<string> ApiVersions { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Interfaces { get; init; } = Array.Empty<string>();
    public DateTimeOffset LastSeenAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
