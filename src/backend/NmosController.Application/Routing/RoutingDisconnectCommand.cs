using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Routing;

public sealed record RoutingDisconnectCommand(
    string DestinationId,
    string RequestedBy,
    bool DisconnectVideo,
    bool DisconnectAudio,
    bool DisconnectAncillary,
    ActivationRequest Activation);
