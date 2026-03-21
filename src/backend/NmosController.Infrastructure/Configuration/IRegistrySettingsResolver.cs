namespace NmosController.Infrastructure.Configuration;

internal interface IRegistrySettingsResolver
{
    Task<ResolvedRegistrySettings> GetAsync(CancellationToken cancellationToken);
}
