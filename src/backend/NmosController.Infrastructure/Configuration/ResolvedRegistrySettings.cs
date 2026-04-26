namespace NmosController.Infrastructure.Configuration;

internal sealed record ResolvedRegistrySettings(
    string Name,
    Uri BaseUrl,
    Uri ConnectionBaseUrl,
    string? ConnectionBaseUrls,
    string QueryApiVersion,
    string ConnectionApiVersion,
    bool IsEnabled);
