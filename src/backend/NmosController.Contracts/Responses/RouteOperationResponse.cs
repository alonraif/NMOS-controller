namespace NmosController.Contracts.Responses;

public sealed record RouteOperationResponse(
    bool Succeeded,
    string? Message);
