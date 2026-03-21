namespace NmosController.Application.Presets;

public sealed record PresetSalvoDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyCollection<PresetRouteDto> Routes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
