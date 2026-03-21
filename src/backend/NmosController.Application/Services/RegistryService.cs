using NmosController.Application.Abstractions.Persistence;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Mappers;
using NmosController.Application.Settings;
using NmosController.Domain.Entities;

namespace NmosController.Application.Services;

public sealed class RegistryService(IRegistryRepository registryRepository) : IRegistryService
{
    public async Task<RegistrySettingsDto?> GetAsync(CancellationToken cancellationToken)
    {
        var registry = await registryRepository.GetPrimaryAsync(cancellationToken);
        return registry?.ToDto();
    }

    public async Task<RegistrySettingsDto> SaveAsync(UpdateRegistrySettingsCommand command, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(command.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Registry base URL is not a valid absolute URI.");
        }

        var existing = await registryRepository.GetPrimaryAsync(cancellationToken);
        var registry = existing ?? new Registry();
        registry.Update(
            command.Name,
            baseUri,
            command.QueryApiVersion,
            command.ConnectionApiVersion,
            command.Mode,
            command.IsEnabled,
            DateTimeOffset.UtcNow);

        await registryRepository.SaveAsync(registry, cancellationToken);
        return registry.ToDto();
    }
}
