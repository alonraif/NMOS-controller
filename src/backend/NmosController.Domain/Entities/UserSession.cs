using NmosController.Domain.Enums;

namespace NmosController.Domain.Entities;

public sealed class UserSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? RemoteAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public SessionState State { get; private set; } = SessionState.Active;

    public void End(DateTimeOffset endedAtUtc, SessionState state)
    {
        EndedAtUtc = endedAtUtc;
        State = state;
    }
}
