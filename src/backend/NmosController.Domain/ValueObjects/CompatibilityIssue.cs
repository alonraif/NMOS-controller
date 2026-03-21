namespace NmosController.Domain.ValueObjects;

public sealed record CompatibilityIssue(
    string Code,
    string Message,
    bool IsBlocking);
