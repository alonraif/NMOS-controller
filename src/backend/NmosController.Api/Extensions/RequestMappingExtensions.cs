using NmosController.Contracts.Requests;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Api.Extensions;

internal static class RequestMappingExtensions
{
    public static ActivationRequest ToActivationRequest(this RouteValidationRequest request) =>
        CreateActivation(request.ActivationMode, request.ActivationTimeUtc, request.RequestedOffsetSeconds);

    public static ActivationRequest ToActivationRequest(this ConnectReceiverRequest request) =>
        CreateActivation(request.ActivationMode, request.ActivationTimeUtc, request.RequestedOffsetSeconds);

    public static ActivationRequest ToActivationRequest(this DisconnectReceiverRequest request) =>
        CreateActivation(request.ActivationMode, request.ActivationTimeUtc, request.RequestedOffsetSeconds);

    public static ActivationRequest ToActivationRequest(this PresetRouteRequest request) =>
        CreateActivation(request.ActivationMode, request.ActivationTimeUtc, request.RequestedOffsetSeconds);

    public static ActivationRequest ToActivationRequest(this ExecutePresetRequest request) =>
        CreateActivation(request.ActivationMode ?? ActivationModeType.Immediate, request.ActivationTimeUtc, request.RequestedOffsetSeconds);

    private static ActivationRequest CreateActivation(
        ActivationModeType activationMode,
        DateTimeOffset? activationTimeUtc,
        double? requestedOffsetSeconds)
    {
        var now = DateTimeOffset.UtcNow;
        return activationMode switch
        {
            ActivationModeType.ScheduledAbsolute when activationTimeUtc.HasValue =>
                ActivationRequest.ScheduledAbsolute(now, activationTimeUtc.Value),
            ActivationModeType.ScheduledRelative when requestedOffsetSeconds.HasValue =>
                ActivationRequest.ScheduledRelative(now, TimeSpan.FromSeconds(requestedOffsetSeconds.Value)),
            _ => ActivationRequest.Immediate(now)
        };
    }
}
