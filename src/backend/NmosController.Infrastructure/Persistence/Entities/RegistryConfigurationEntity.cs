namespace NmosController.Infrastructure.Persistence.Entities;

public sealed class RegistryConfigurationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ConnectionBaseUrl { get; set; }
    public string? ConnectionBaseUrls { get; set; }
    public string QueryApiVersion { get; set; } = "v1.3";
    public string ConnectionApiVersion { get; set; } = "v1.1";
    public bool IsEnabled { get; set; } = true;
    public bool InitialSetupCompleted { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
