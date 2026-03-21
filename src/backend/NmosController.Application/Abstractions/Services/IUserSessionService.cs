using NmosController.Application.Sessions;

namespace NmosController.Application.Abstractions.Services;

public interface IUserSessionService
{
    Task<UserSessionDto> StartAsync(StartUserSessionCommand command, CancellationToken cancellationToken);
    Task EndAsync(Guid sessionId, EndUserSessionCommand command, CancellationToken cancellationToken);
}
