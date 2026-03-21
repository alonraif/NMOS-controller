using NmosController.Domain.Entities;

namespace NmosController.Application.Abstractions.Persistence;

public interface IAlarmRepository
{
    Task<IReadOnlyCollection<AlarmEvent>> GetOpenAsync(CancellationToken cancellationToken);
    Task AddAsync(AlarmEvent alarm, CancellationToken cancellationToken);
}
