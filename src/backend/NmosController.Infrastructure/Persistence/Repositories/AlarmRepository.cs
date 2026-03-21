using Microsoft.EntityFrameworkCore;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Domain.Entities;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Repositories;

public sealed class AlarmRepository(ControllerDbContext dbContext) : IAlarmRepository
{
    public async Task<IReadOnlyCollection<AlarmEvent>> GetOpenAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.AlarmEvents
            .AsNoTracking()
            .Where(x => x.ClearedAtUtc == null)
            .OrderByDescending(x => x.RaisedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => new AlarmEvent
        {
            Id = entity.Id,
            Severity = entity.Severity,
            Code = entity.Code,
            Message = entity.Message,
            ResourceId = entity.ResourceId,
            RaisedAtUtc = entity.RaisedAtUtc
        }).ToArray();
    }

    public async Task AddAsync(AlarmEvent alarm, CancellationToken cancellationToken)
    {
        dbContext.AlarmEvents.Add(new AlarmEventEntity
        {
            Id = alarm.Id,
            Severity = alarm.Severity,
            Code = alarm.Code,
            Message = alarm.Message,
            ResourceId = alarm.ResourceId,
            RaisedAtUtc = alarm.RaisedAtUtc,
            ClearedAtUtc = alarm.ClearedAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
