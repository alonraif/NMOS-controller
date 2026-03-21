using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Routing;

public sealed record RouteValidationCommand(
    string ReceiverId,
    string SenderId,
    ActivationRequest Activation);
