using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Topology;

public sealed record NmosReceiverDto(
    string Id,
    string NodeId,
    string DeviceId,
    string Label,
    NmosTransportType Transport,
    MediaFormatSummary Format,
    ConstraintSet Constraints,
    ConnectionState Active,
    ConnectionState Staged,
    bool IsConnectable,
    string SignalType,
    string RoutingDestinationId,
    string RoutingDestinationLabel,
    DateTimeOffset LastSeenAtUtc);
