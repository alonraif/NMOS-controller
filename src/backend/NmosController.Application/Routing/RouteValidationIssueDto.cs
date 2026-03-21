namespace NmosController.Application.Routing;

public sealed record RouteValidationIssueDto(
    string Code,
    string Message,
    bool IsBlocking);
