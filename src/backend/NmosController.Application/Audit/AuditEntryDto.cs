using NmosController.Domain.Enums;

namespace NmosController.Application.Audit;

public sealed record AuditEntryDto(
    Guid Id,
    AuditActionType ActionType,
    string Actor,
    string Summary,
    string? ResourceId,
    string? ResourceType,
    string? CorrelationId,
    DateTimeOffset OccurredAtUtc,
    string? MetadataJson);
