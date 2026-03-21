using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Persistence.Entities;

public sealed class AuditEntryEntity
{
    public Guid Id { get; set; }
    public AuditActionType ActionType { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
