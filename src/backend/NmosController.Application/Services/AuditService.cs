using NmosController.Application.Abstractions.Persistence;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Mappers;
using NmosController.Domain.Entities;

namespace NmosController.Application.Services;

public sealed class AuditService(IAuditRepository auditRepository) : IAuditService
{
    public async Task<IReadOnlyCollection<AuditEntryDto>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var entries = await auditRepository.GetRecentAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return entries.Select(x => x.ToDto()).ToArray();
    }

    public Task RecordAsync(CreateAuditEntryCommand command, CancellationToken cancellationToken)
    {
        var entry = new AuditEntry
        {
            ActionType = command.ActionType,
            Actor = command.Actor,
            Summary = command.Summary,
            ResourceId = command.ResourceId,
            ResourceType = command.ResourceType,
            CorrelationId = command.CorrelationId,
            MetadataJson = command.MetadataJson,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        return auditRepository.AddAsync(entry, cancellationToken);
    }
}
