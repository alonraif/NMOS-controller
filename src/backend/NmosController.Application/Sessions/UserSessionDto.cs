using NmosController.Domain.Enums;

namespace NmosController.Application.Sessions;

public sealed record UserSessionDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string? RemoteAddress,
    string? UserAgent,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    SessionState State);
