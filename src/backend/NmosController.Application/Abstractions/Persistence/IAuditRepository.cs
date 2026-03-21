using NmosController.Domain.Entities;

namespace NmosController.Application.Abstractions.Persistence;

public interface IAuditRepository
{
    Task<IReadOnlyCollection<AuditEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);
}
