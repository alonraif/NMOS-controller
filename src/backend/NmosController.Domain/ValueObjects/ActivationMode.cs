using NmosController.Domain.Enums;

namespace NmosController.Domain.ValueObjects;

public sealed record ActivationMode(
    ActivationModeType Type,
    DateTimeOffset? ActivationTimeUtc = null,
    TimeSpan? RequestedOffset = null)
{
    public static ActivationMode Immediate() => new(ActivationModeType.Immediate);

    public static ActivationMode ScheduledAbsolute(DateTimeOffset activationTimeUtc) =>
        new(ActivationModeType.ScheduledAbsolute, activationTimeUtc);

    public static ActivationMode ScheduledRelative(TimeSpan requestedOffset) =>
        new(ActivationModeType.ScheduledRelative, null, requestedOffset);
}
