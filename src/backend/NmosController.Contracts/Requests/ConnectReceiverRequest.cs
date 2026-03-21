using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class ConnectReceiverRequest
{
    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string RequestedBy { get; set; } = "operator";

    public ActivationModeType ActivationMode { get; set; } = ActivationModeType.Immediate;

    public DateTimeOffset? ActivationTimeUtc { get; set; }

    public double? RequestedOffsetSeconds { get; set; }
}
