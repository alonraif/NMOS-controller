using Microsoft.EntityFrameworkCore;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Domain.Entities;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(ControllerDbContext dbContext) : IAuditRepository
{
    public async Task<IReadOnlyCollection<AuditEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var entities = await dbContext.AuditEntries
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        dbContext.AuditEntries.Add(new AuditEntryEntity
        {
            Id = entry.Id,
            ActionType = entry.ActionType,
            Actor = entry.Actor,
            Summary = entry.Summary,
            ResourceId = entry.ResourceId,
            ResourceType = entry.ResourceType,
            CorrelationId = entry.CorrelationId,
            MetadataJson = entry.MetadataJson,
            OccurredAtUtc = entry.OccurredAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AuditEntry Map(AuditEntryEntity entity) =>
        new()
        {
            Id = entity.Id,
            ActionType = entity.ActionType,
            Actor = entity.Actor,
            Summary = entity.Summary,
            ResourceId = entity.ResourceId,
            ResourceType = entity.ResourceType,
            CorrelationId = entity.CorrelationId,
            MetadataJson = entity.MetadataJson,
            OccurredAtUtc = entity.OccurredAtUtc
        };
}
