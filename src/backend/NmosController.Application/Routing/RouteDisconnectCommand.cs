using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Routing;

public sealed record RouteDisconnectCommand(
    string ReceiverId,
    string RequestedBy,
    ActivationRequest Activation);
