using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class ExecutePresetRequest
{
    [Required]
    public string RequestedBy { get; set; } = "operator";

    public ActivationModeType? ActivationMode { get; set; }

    public DateTimeOffset? ActivationTimeUtc { get; set; }

    public double? RequestedOffsetSeconds { get; set; }
}
