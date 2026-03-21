namespace NmosController.Domain.ValueObjects;

public sealed record ConstraintParameter(
    string Name,
    string? Minimum,
    string? Maximum,
    IReadOnlyCollection<string> AllowedValues);
