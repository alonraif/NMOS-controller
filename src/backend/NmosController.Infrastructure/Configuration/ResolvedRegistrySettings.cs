using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Configuration;

internal sealed record ResolvedRegistrySettings(
    string Name,
    Uri BaseUrl,
    Uri ConnectionBaseUrl,
    string? ConnectionBaseUrls,
    string QueryApiVersion,
    string ConnectionApiVersion,
    ControllerMode Mode,
    bool IsEnabled);
