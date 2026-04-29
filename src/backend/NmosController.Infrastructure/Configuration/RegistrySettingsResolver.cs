using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmosController.Application.Settings;
using NmosController.Infrastructure.Persistence;

namespace NmosController.Infrastructure.Configuration;

internal sealed class RegistrySettingsResolver(
    ControllerDbContext dbContext,
    IOptionsMonitor<NmosControllerOptions> optionsMonitor,
    IMdnsRegistryDiscovery mdnsRegistryDiscovery,
    ILogger<RegistrySettingsResolver> logger) : IRegistrySettingsResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ResolvedRegistrySettings? _cachedSettings;
    private DateTimeOffset _cachedAtUtc;

    public async Task<ResolvedRegistrySettings> GetAsync(CancellationToken cancellationToken)
    {
        if (_cachedSettings is not null && DateTimeOffset.UtcNow - _cachedAtUtc < CacheDuration)
        {
            return _cachedSettings;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedSettings is not null && DateTimeOffset.UtcNow - _cachedAtUtc < CacheDuration)
            {
                return _cachedSettings;
            }

            _cachedSettings = await ResolveSettingsAsync(cancellationToken);
            _cachedAtUtc = DateTimeOffset.UtcNow;
            return _cachedSettings;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ResolvedRegistrySettings> ResolveSettingsAsync(CancellationToken cancellationToken)
    {
        var registry = await dbContext.Registries
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (registry is not null && Uri.TryCreate(registry.BaseUrl, UriKind.Absolute, out var persistedUri))
        {
            var configuredConnectionUri = ResolveConnectionUri(registry.ConnectionBaseUrl ?? optionsMonitor.CurrentValue.Registry.ConnectionBaseUrl, persistedUri);
            return await ResolveWithDiscoveryAsync(new ResolvedRegistrySettings(
                registry.Name,
                persistedUri,
                registry.DiscoveryMode,
                null,
                configuredConnectionUri,
                registry.ConnectionBaseUrls ?? optionsMonitor.CurrentValue.Registry.ConnectionBaseUrls,
                registry.QueryApiVersion,
                registry.ConnectionApiVersion,
                registry.IsEnabled), registry.MdnsQueryServiceType, registry.MdnsResolveTimeoutMilliseconds, cancellationToken);
        }

        var options = optionsMonitor.CurrentValue;
        var baseUri = Uri.TryCreate(options.Registry.BaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri
            : new Uri("http://localhost:8081");

        return await ResolveWithDiscoveryAsync(new ResolvedRegistrySettings(
            options.Registry.Name,
            baseUri,
            options.Registry.DiscoveryMode,
            null,
            ResolveConnectionUri(options.Registry.ConnectionBaseUrl, baseUri),
            options.Registry.ConnectionBaseUrls,
            options.Registry.QueryApiVersion,
            options.Registry.ConnectionApiVersion,
            options.Registry.IsEnabled), options.Registry.MdnsQueryServiceType, options.Registry.MdnsResolveTimeoutMilliseconds, cancellationToken);
    }

    private async Task<ResolvedRegistrySettings> ResolveWithDiscoveryAsync(
        ResolvedRegistrySettings configuredSettings,
        string mdnsQueryServiceType,
        int mdnsResolveTimeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(configuredSettings.DiscoveryMode, RegistryDiscoveryMode.Mdns, StringComparison.OrdinalIgnoreCase))
        {
            return configuredSettings with { DiscoveryMode = RegistryDiscoveryMode.Manual };
        }

        var timeout = TimeSpan.FromMilliseconds(Math.Clamp(mdnsResolveTimeoutMilliseconds, 250, 15000));
        var discovery = await mdnsRegistryDiscovery.DiscoverAsync(mdnsQueryServiceType, timeout, cancellationToken);
        if (discovery is null)
        {
            logger.LogInformation("mDNS discovery returned no endpoint. Falling back to configured registry base URL {BaseUrl}.", configuredSettings.BaseUrl);
            return configuredSettings with { DiscoveryMode = RegistryDiscoveryMode.Mdns };
        }

        var queryApiVersion = !string.IsNullOrWhiteSpace(discovery.QueryApiVersion)
            ? discovery.QueryApiVersion!
            : configuredSettings.QueryApiVersion;

        return configuredSettings with
        {
            BaseUrl = discovery.QueryBaseUrl,
            ConnectionBaseUrl = configuredSettings.ConnectionBaseUrl == configuredSettings.BaseUrl
                ? discovery.QueryBaseUrl
                : configuredSettings.ConnectionBaseUrl,
            QueryApiVersion = queryApiVersion,
            DiscoveryMode = RegistryDiscoveryMode.Mdns,
            DiscoveredAtUtc = discovery.DiscoveredAtUtc
        };
    }

    private static Uri ResolveConnectionUri(string? configuredConnectionBaseUrl, Uri fallbackBaseUri) =>
        Uri.TryCreate(configuredConnectionBaseUrl, UriKind.Absolute, out var configuredConnectionUri)
            ? configuredConnectionUri
            : fallbackBaseUri;
}
