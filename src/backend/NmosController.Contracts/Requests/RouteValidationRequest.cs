using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class RouteValidationRequest
{
    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    public ActivationModeType ActivationMode { get; set; } = ActivationModeType.Immediate;

    public DateTimeOffset? ActivationTimeUtc { get; set; }

    public double? RequestedOffsetSeconds { get; set; }
}
