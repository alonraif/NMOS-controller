using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class RoutingConnectRequest
{
    [Required]
    public string DestinationId { get; set; } = string.Empty;

    [Required]
    public string RequestedBy { get; set; } = "operator";

    public string? VideoSourceId { get; set; }

    public string? AudioSourceId { get; set; }

    public string? AncillarySourceId { get; set; }

    public ActivationModeType ActivationMode { get; set; } = ActivationModeType.Immediate;

    public DateTimeOffset? ActivationTimeUtc { get; set; }

    public double? RequestedOffsetSeconds { get; set; }
}
