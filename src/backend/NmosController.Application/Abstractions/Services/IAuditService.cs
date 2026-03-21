using NmosController.Application.Audit;

namespace NmosController.Application.Abstractions.Services;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditEntryDto>> GetRecentAsync(int limit, CancellationToken cancellationToken);
    Task RecordAsync(CreateAuditEntryCommand command, CancellationToken cancellationToken);
}
