namespace NmosController.Domain.Entities;

public sealed class NmosDevice
{
    public string Id { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string NodeId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public IReadOnlyCollection<string> SenderIds { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ReceiverIds { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public DateTimeOffset LastSeenAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
