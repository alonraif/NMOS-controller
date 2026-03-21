using NmosController.Domain.Entities;

namespace NmosController.Application.Abstractions.Persistence;

public interface IPresetSalvoRepository
{
    Task<IReadOnlyCollection<PresetSalvo>> GetAllAsync(CancellationToken cancellationToken);
    Task<PresetSalvo?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(PresetSalvo preset, CancellationToken cancellationToken);
    Task UpdateAsync(PresetSalvo preset, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
