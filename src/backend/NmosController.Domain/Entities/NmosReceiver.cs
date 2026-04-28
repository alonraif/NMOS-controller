using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Entities;

public sealed class NmosReceiver
{
    public string Id { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string NodeId { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public NmosTransportType Transport { get; init; }
    public MediaFormatSummary Format { get; init; } = new(string.Empty, null, null, null, null, null);
    public ConstraintSet Constraints { get; init; } = ConstraintSet.Empty;
    public ConnectionState Active { get; init; } = new(null, null, new Dictionary<string, string>(), null);
    public ConnectionState Staged { get; init; } = new(null, null, new Dictionary<string, string>(), null);
    public bool IsConnectable { get; init; } = true;
    public IReadOnlyCollection<string> InterfaceBindings { get; init; } = Array.Empty<string>();
    public string? ConnectionApiBaseUrl { get; init; }
    public DateTimeOffset LastSeenAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IsConnected => !string.IsNullOrWhiteSpace(Active.SenderId);
}
