using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Entities;

public sealed class NmosFlow
{
    public string Id { get; init; } = string.Empty;
    public string RegistryId { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public MediaFormatSummary Format { get; init; } = new(string.Empty, null, null, null, null, null);
    public IReadOnlyCollection<string> Parents { get; init; } = Array.Empty<string>();
    public DateTimeOffset LastSeenAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
