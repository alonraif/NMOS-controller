using Microsoft.EntityFrameworkCore;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Domain.Entities;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Repositories;

public sealed class UserSessionRepository(ControllerDbContext dbContext) : IUserSessionRepository
{
    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var session = new UserSession
        {
            Id = entity.Id,
            UserName = entity.UserName,
            DisplayName = entity.DisplayName,
            RemoteAddress = entity.RemoteAddress,
            UserAgent = entity.UserAgent,
            StartedAtUtc = entity.StartedAtUtc
        };

        if (entity.EndedAtUtc.HasValue || entity.State != Domain.Enums.SessionState.Active)
        {
            session.End(entity.EndedAtUtc ?? entity.StartedAtUtc, entity.State);
        }

        return session;
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        dbContext.UserSessions.Add(new UserSessionEntity
        {
            Id = session.Id,
            UserName = session.UserName,
            DisplayName = session.DisplayName,
            RemoteAddress = session.RemoteAddress,
            UserAgent = session.UserAgent,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            State = session.State
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserSessions.SingleAsync(x => x.Id == session.Id, cancellationToken);
        entity.EndedAtUtc = session.EndedAtUtc;
        entity.State = session.State;
        entity.DisplayName = session.DisplayName;
        entity.RemoteAddress = session.RemoteAddress;
        entity.UserAgent = session.UserAgent;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
