using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Persistence.Entities;

public sealed class UserSessionEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? RemoteAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public SessionState State { get; set; }
}
