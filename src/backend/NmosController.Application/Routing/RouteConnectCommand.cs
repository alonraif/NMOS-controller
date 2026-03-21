using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Routing;

public sealed record RouteConnectCommand(
    string ReceiverId,
    string SenderId,
    string RequestedBy,
    ActivationRequest Activation);
