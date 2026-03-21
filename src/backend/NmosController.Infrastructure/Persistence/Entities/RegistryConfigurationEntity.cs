using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Persistence.Entities;

public sealed class RegistryConfigurationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string QueryApiVersion { get; set; } = "v1.3";
    public string ConnectionApiVersion { get; set; } = "v1.1";
    public ControllerMode Mode { get; set; } = ControllerMode.Mock;
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
