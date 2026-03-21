using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Configuration;

internal sealed record ResolvedRegistrySettings(
    string Name,
    Uri BaseUrl,
    string QueryApiVersion,
    string ConnectionApiVersion,
    ControllerMode Mode,
    bool IsEnabled);
