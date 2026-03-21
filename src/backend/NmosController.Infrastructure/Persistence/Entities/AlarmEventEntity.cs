using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Persistence.Entities;

public sealed class AlarmEventEntity
{
    public Guid Id { get; set; }
    public AlarmSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public DateTimeOffset RaisedAtUtc { get; set; }
    public DateTimeOffset? ClearedAtUtc { get; set; }
}
