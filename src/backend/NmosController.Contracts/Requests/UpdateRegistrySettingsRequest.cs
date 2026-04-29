using System.ComponentModel.DataAnnotations;
namespace NmosController.Contracts.Requests;

public sealed class UpdateRegistrySettingsRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string DiscoveryMode { get; set; } = "Manual";

    [Required]
    [MaxLength(128)]
    public string MdnsQueryServiceType { get; set; } = "_nmos-query._tcp.local.";

    [Range(250, 15000)]
    public int MdnsResolveTimeoutMilliseconds { get; set; } = 2000;

    [Url]
    public string? ConnectionBaseUrl { get; set; }

    public string? ConnectionBaseUrls { get; set; }

    [Required]
    public string QueryApiVersion { get; set; } = "v1.3";

    [Required]
    public string ConnectionApiVersion { get; set; } = "v1.1";

    public bool IsEnabled { get; set; } = true;
    public bool? InitialSetupCompleted { get; set; }
}
