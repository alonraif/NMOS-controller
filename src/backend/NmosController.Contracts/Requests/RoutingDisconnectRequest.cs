using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class RoutingDisconnectRequest
{
    [Required]
    public string DestinationId { get; set; } = string.Empty;

    [Required]
    public string RequestedBy { get; set; } = "operator";

    public bool DisconnectVideo { get; set; } = true;

    public bool DisconnectAudio { get; set; } = true;

    public bool DisconnectAncillary { get; set; } = true;

    public ActivationModeType ActivationMode { get; set; } = ActivationModeType.Immediate;

    public DateTimeOffset? ActivationTimeUtc { get; set; }

    public double? RequestedOffsetSeconds { get; set; }
}
