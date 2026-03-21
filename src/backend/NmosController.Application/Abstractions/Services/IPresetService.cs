using NmosController.Application.Common;
using NmosController.Application.Presets;

namespace NmosController.Application.Abstractions.Services;

public interface IPresetService
{
    Task<IReadOnlyCollection<PresetSalvoDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PresetSalvoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> SaveAsync(UpsertPresetSalvoCommand command, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ServiceResult> ExecuteAsync(ExecutePresetSalvoCommand command, CancellationToken cancellationToken);
}
