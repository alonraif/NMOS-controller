using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NmosController.Infrastructure.Persistence;

namespace NmosController.Infrastructure.Configuration;

internal sealed class RegistrySettingsResolver(
    ControllerDbContext dbContext,
    IOptionsMonitor<NmosControllerOptions> optionsMonitor) : IRegistrySettingsResolver
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ResolvedRegistrySettings? _cachedSettings;

    public async Task<ResolvedRegistrySettings> GetAsync(CancellationToken cancellationToken)
    {
        if (_cachedSettings is not null)
        {
            return _cachedSettings;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedSettings is not null)
            {
                return _cachedSettings;
            }

            _cachedSettings = await ResolveSettingsAsync(cancellationToken);
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
            return new ResolvedRegistrySettings(
                registry.Name,
                persistedUri,
                configuredConnectionUri,
                registry.ConnectionBaseUrls ?? optionsMonitor.CurrentValue.Registry.ConnectionBaseUrls,
                registry.QueryApiVersion,
                registry.ConnectionApiVersion,
                registry.IsEnabled);
        }

        var options = optionsMonitor.CurrentValue;
        var baseUri = Uri.TryCreate(options.Registry.BaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri
            : new Uri("http://localhost:8081");

        return new ResolvedRegistrySettings(
            options.Registry.Name,
            baseUri,
            ResolveConnectionUri(options.Registry.ConnectionBaseUrl, baseUri),
            options.Registry.ConnectionBaseUrls,
            options.Registry.QueryApiVersion,
            options.Registry.ConnectionApiVersion,
            options.Registry.IsEnabled);
    }

    private static Uri ResolveConnectionUri(string? configuredConnectionBaseUrl, Uri fallbackBaseUri) =>
        Uri.TryCreate(configuredConnectionBaseUrl, UriKind.Absolute, out var configuredConnectionUri)
            ? configuredConnectionUri
            : fallbackBaseUri;
}
