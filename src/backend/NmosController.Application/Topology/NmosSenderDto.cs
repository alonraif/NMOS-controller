using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Topology;

public sealed record NmosSenderDto(
    string Id,
    string NodeId,
    string DeviceId,
    string? FlowId,
    string Label,
    NmosTransportType Transport,
    MediaFormatSummary Format,
    string? ManifestHref,
    string? SubscribedReceiverId,
    TransportFileData? TransportFile,
    string SignalType,
    string SourceGroupId,
    string SourceGroupLabel,
    string? RedundancyGroupId,
    string PathType,
    bool IsHealthy,
    DateTimeOffset LastSeenAtUtc);
