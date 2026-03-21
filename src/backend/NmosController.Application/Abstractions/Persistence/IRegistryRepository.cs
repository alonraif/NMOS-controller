using NmosController.Domain.Entities;

namespace NmosController.Application.Abstractions.Persistence;

public interface IRegistryRepository
{
    Task<Registry?> GetPrimaryAsync(CancellationToken cancellationToken);
    Task SaveAsync(Registry registry, CancellationToken cancellationToken);
}
