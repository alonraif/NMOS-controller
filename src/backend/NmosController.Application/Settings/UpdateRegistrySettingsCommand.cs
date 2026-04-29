namespace NmosController.Application.Settings;

public sealed record UpdateRegistrySettingsCommand(
    string Name,
    string BaseUrl,
    string DiscoveryMode,
    string MdnsQueryServiceType,
    int MdnsResolveTimeoutMilliseconds,
    string? ConnectionBaseUrl,
    string? ConnectionBaseUrls,
    string QueryApiVersion,
    string ConnectionApiVersion,
    bool IsEnabled,
    bool? InitialSetupCompleted);
