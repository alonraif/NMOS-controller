using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Entities;

public sealed class NmosSender
{
    public string Id { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string NodeId { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public string? FlowId { get; init; }
    public string Label { get; init; } = string.Empty;
    public NmosTransportType Transport { get; init; }
    public MediaFormatSummary Format { get; init; } = new(string.Empty, null, null, null, null, null);
    public string? ManifestHref { get; init; }
    public string? SubscribedReceiverId { get; init; }
    public IReadOnlyCollection<string> InterfaceBindings { get; init; } = Array.Empty<string>();
    public TransportFileData? TransportFile { get; init; }
    public DateTimeOffset LastSeenAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
