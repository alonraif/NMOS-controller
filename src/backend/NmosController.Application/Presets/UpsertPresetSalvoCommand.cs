using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Presets;

public sealed record UpsertPresetSalvoCommand(
    Guid? Id,
    string Name,
    string? Description,
    IReadOnlyCollection<PresetRoute> Routes);
