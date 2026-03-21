using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Routing;

public sealed record RoutingConnectCommand(
    string DestinationId,
    string RequestedBy,
    string? VideoSourceId,
    string? AudioSourceId,
    string? AncillarySourceId,
    ActivationRequest Activation);
