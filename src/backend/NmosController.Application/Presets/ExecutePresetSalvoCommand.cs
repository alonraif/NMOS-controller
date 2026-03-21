using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Presets;

public sealed record ExecutePresetSalvoCommand(
    Guid PresetId,
    string RequestedBy,
    ActivationRequest? OverrideActivation);
