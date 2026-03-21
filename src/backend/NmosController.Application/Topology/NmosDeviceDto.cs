namespace NmosController.Application.Topology;

public sealed record NmosDeviceDto(
    string Id,
    string NodeId,
    string Label,
    string DeviceType,
    IReadOnlyCollection<string> SenderIds,
    IReadOnlyCollection<string> ReceiverIds,
    DateTimeOffset LastSeenAtUtc);
