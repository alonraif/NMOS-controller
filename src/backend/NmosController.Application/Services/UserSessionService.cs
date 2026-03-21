using NmosController.Application.Abstractions.Persistence;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Mappers;
using NmosController.Application.Sessions;
using NmosController.Domain.Entities;

namespace NmosController.Application.Services;

public sealed class UserSessionService(IUserSessionRepository sessionRepository) : IUserSessionService
{
    public async Task<UserSessionDto> StartAsync(StartUserSessionCommand command, CancellationToken cancellationToken)
    {
        var session = new UserSession
        {
            UserName = command.UserName,
            DisplayName = command.DisplayName,
            RemoteAddress = command.RemoteAddress,
            UserAgent = command.UserAgent,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        await sessionRepository.AddAsync(session, cancellationToken);
        return session.ToDto();
    }

    public async Task EndAsync(Guid sessionId, EndUserSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"User session '{sessionId}' was not found.");

        session.End(DateTimeOffset.UtcNow, command.State);
        await sessionRepository.UpdateAsync(session, cancellationToken);
    }
}
