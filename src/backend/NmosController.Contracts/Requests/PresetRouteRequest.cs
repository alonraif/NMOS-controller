using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class PresetRouteRequest
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    public string? SenderId { get; set; }

    public ActivationModeType ActivationMode { get; set; } = ActivationModeType.Immediate;

    public DateTimeOffset? ActivationTimeUtc { get; set; }

    public double? RequestedOffsetSeconds { get; set; }
}
