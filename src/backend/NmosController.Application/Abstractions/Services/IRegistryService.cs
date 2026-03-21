using NmosController.Application.Settings;

namespace NmosController.Application.Abstractions.Services;

public interface IRegistryService
{
    Task<RegistrySettingsDto?> GetAsync(CancellationToken cancellationToken);
    Task<RegistrySettingsDto> SaveAsync(UpdateRegistrySettingsCommand command, CancellationToken cancellationToken);
}
