using NmosController.Domain.Enums;

namespace NmosController.Application.Routing;

public sealed record RouteValidationResultDto(
    CompatibilityStatus Status,
    IReadOnlyCollection<RouteValidationIssueDto> Issues);
