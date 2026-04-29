namespace NmosController.Infrastructure.Configuration;

internal sealed record ResolvedRegistrySettings(
    string Name,
    Uri BaseUrl,
    string DiscoveryMode,
    DateTimeOffset? DiscoveredAtUtc,
    Uri ConnectionBaseUrl,
    string? ConnectionBaseUrls,
    string QueryApiVersion,
    string ConnectionApiVersion,
    bool IsEnabled);
