using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Topology;

public sealed record NmosSourceDto(
    string Id,
    string DeviceId,
    string Label,
    MediaFormatSummary Format,
    DateTimeOffset LastSeenAtUtc);
