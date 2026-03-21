using NmosController.Domain.Enums;

namespace NmosController.Domain.Entities;

public sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AuditActionType ActionType { get; init; }
    public string Actor { get; init; } = "system";
    public string Summary { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public string? ResourceType { get; init; }
    public string? CorrelationId { get; init; }
    public string? MetadataJson { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
