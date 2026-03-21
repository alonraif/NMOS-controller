using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Topology;

public sealed record NmosFlowDto(
    string Id,
    string SourceId,
    string DeviceId,
    string Label,
    MediaFormatSummary Format,
    DateTimeOffset LastSeenAtUtc);
