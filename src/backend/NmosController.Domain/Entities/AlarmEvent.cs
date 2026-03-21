using NmosController.Domain.Enums;

namespace NmosController.Domain.Entities;

public sealed class AlarmEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AlarmSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public DateTimeOffset RaisedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClearedAtUtc { get; private set; }

    public void Clear(DateTimeOffset clearedAtUtc) => ClearedAtUtc = clearedAtUtc;
}
