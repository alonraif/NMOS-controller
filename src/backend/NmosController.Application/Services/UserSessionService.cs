using System.Text.Json;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Mappers;
using NmosController.Application.Sessions;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;

namespace NmosController.Application.Services;

public sealed class UserSessionService(
    IUserSessionRepository sessionRepository,
    IAuditService auditService) : IUserSessionService
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
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.UserSessionStarted,
                command.UserName,
                $"User session started for '{command.DisplayName ?? command.UserName}'.",
                session.Id.ToString(),
                "UserSession",
                null,
                JsonSerializer.Serialize(new { command.UserName, command.DisplayName, command.RemoteAddress })),
            cancellationToken);
        return session.ToDto();
    }

    public async Task EndAsync(Guid sessionId, EndUserSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"User session '{sessionId}' was not found.");

        session.End(DateTimeOffset.UtcNow, command.State);
        await sessionRepository.UpdateAsync(session, cancellationToken);
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.UserSessionEnded,
                session.UserName,
                $"User session ended for '{session.DisplayName ?? session.UserName}'.",
                session.Id.ToString(),
                "UserSession",
                null,
                JsonSerializer.Serialize(new { SessionId = session.Id, command.State })),
            cancellationToken);
    }
}
