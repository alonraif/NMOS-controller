using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NmosController.Infrastructure.Persistence;

namespace NmosController.Infrastructure.Configuration;

internal sealed class RegistrySettingsResolver(
    ControllerDbContext dbContext,
    IOptionsMonitor<NmosControllerOptions> optionsMonitor) : IRegistrySettingsResolver
{
    public async Task<ResolvedRegistrySettings> GetAsync(CancellationToken cancellationToken)
    {
        var registry = await dbContext.Registries
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (registry is not null && Uri.TryCreate(registry.BaseUrl, UriKind.Absolute, out var persistedUri))
        {
            return new ResolvedRegistrySettings(
                registry.Name,
                persistedUri,
                registry.QueryApiVersion,
                registry.ConnectionApiVersion,
                registry.Mode,
                registry.IsEnabled);
        }

        var options = optionsMonitor.CurrentValue;
        var baseUri = Uri.TryCreate(options.Registry.BaseUrl, UriKind.Absolute, out var configuredUri)
            ? configuredUri
            : new Uri("http://localhost:8081");

        return new ResolvedRegistrySettings(
            options.Registry.Name,
            baseUri,
            options.Registry.QueryApiVersion,
            options.Registry.ConnectionApiVersion,
            options.Mode,
            options.Registry.IsEnabled);
    }
}
