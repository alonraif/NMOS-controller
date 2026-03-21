using NmosController.Domain.Enums;

namespace NmosController.Application.Audit;

public sealed record CreateAuditEntryCommand(
    AuditActionType ActionType,
    string Actor,
    string Summary,
    string? ResourceId,
    string? ResourceType,
    string? CorrelationId,
    string? MetadataJson);
