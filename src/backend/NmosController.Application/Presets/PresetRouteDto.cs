using NmosController.Domain.Enums;

namespace NmosController.Application.Presets;

public sealed record PresetRouteDto(
    string ReceiverId,
    string? SenderId,
    ActivationModeType ActivationMode,
    DateTimeOffset? ActivationTimeUtc,
    TimeSpan? RequestedOffset);
