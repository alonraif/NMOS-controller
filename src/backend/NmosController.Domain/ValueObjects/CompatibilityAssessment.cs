using NmosController.Domain.Enums;

namespace NmosController.Domain.ValueObjects;

public sealed record CompatibilityAssessment(
    CompatibilityStatus Status,
    IReadOnlyCollection<CompatibilityIssue> Issues)
{
    public static CompatibilityAssessment Compatible() =>
        new(CompatibilityStatus.Compatible, Array.Empty<CompatibilityIssue>());
}
