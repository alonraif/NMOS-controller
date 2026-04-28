using Microsoft.EntityFrameworkCore;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Domain.Entities;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Repositories;

public sealed class RegistryRepository(ControllerDbContext dbContext) : IRegistryRepository
{
    public async Task<Registry?> GetPrimaryAsync(CancellationToken cancellationToken)
    {
        var entity = await dbContext.Registries
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null || !Uri.TryCreate(entity.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        var registry = new Registry
        {
            Id = entity.Id
        };

        registry.Update(
            entity.Name,
            baseUri,
            entity.QueryApiVersion,
            entity.ConnectionApiVersion,
            entity.IsEnabled,
            entity.UpdatedAtUtc,
            entity.ConnectionBaseUrl,
            entity.ConnectionBaseUrls,
            entity.InitialSetupCompleted);

        return registry;
    }

    public async Task SaveAsync(Registry registry, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Registries
            .SingleOrDefaultAsync(x => x.Id == registry.Id, cancellationToken);

        if (existing is null)
        {
            dbContext.Registries.Add(new RegistryConfigurationEntity
            {
                Id = registry.Id,
                Name = registry.Name,
                BaseUrl = registry.BaseUrl.ToString(),
                ConnectionBaseUrl = registry.ConnectionBaseUrl,
                ConnectionBaseUrls = registry.ConnectionBaseUrls,
                QueryApiVersion = registry.QueryApiVersion,
                ConnectionApiVersion = registry.ConnectionApiVersion,
                IsEnabled = registry.IsEnabled,
                InitialSetupCompleted = registry.InitialSetupCompleted,
                UpdatedAtUtc = registry.UpdatedAtUtc
            });
        }
        else
        {
            existing.Name = registry.Name;
            existing.BaseUrl = registry.BaseUrl.ToString();
            existing.ConnectionBaseUrl = registry.ConnectionBaseUrl;
            existing.ConnectionBaseUrls = registry.ConnectionBaseUrls;
            existing.QueryApiVersion = registry.QueryApiVersion;
            existing.ConnectionApiVersion = registry.ConnectionApiVersion;
            existing.IsEnabled = registry.IsEnabled;
            existing.InitialSetupCompleted = registry.InitialSetupCompleted;
            existing.UpdatedAtUtc = registry.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
