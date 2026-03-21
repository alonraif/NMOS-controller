namespace NmosController.Domain.ValueObjects;

public sealed record PresetRoute(
    string ReceiverId,
    string? SenderId,
    ActivationRequest Activation);
