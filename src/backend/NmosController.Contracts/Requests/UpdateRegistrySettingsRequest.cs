using System.ComponentModel.DataAnnotations;
using NmosController.Domain.Enums;

namespace NmosController.Contracts.Requests;

public sealed class UpdateRegistrySettingsRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Url]
    public string? ConnectionBaseUrl { get; set; }

    public string? ConnectionBaseUrls { get; set; }

    [Required]
    public string QueryApiVersion { get; set; } = "v1.3";

    [Required]
    public string ConnectionApiVersion { get; set; } = "v1.1";

    public ControllerMode Mode { get; set; } = ControllerMode.Mock;

    public bool IsEnabled { get; set; } = true;
}
