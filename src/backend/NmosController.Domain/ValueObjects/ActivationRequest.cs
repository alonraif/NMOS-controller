using NmosController.Domain.Enums;

namespace NmosController.Domain.ValueObjects;

public sealed record ActivationRequest(
    ActivationModeType Mode,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ActivationTimeUtc = null,
    TimeSpan? RequestedOffset = null)
{
    public static ActivationRequest Immediate(DateTimeOffset requestedAtUtc) =>
        new(ActivationModeType.Immediate, requestedAtUtc);

    public static ActivationRequest ScheduledAbsolute(DateTimeOffset requestedAtUtc, DateTimeOffset activationTimeUtc) =>
        new(ActivationModeType.ScheduledAbsolute, requestedAtUtc, activationTimeUtc);

    public static ActivationRequest ScheduledRelative(DateTimeOffset requestedAtUtc, TimeSpan offset) =>
        new(ActivationModeType.ScheduledRelative, requestedAtUtc, null, offset);
}
