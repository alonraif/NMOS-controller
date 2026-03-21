namespace NmosController.Application.Common;

public sealed record ServiceResult(
    bool Succeeded,
    string? Message = null)
{
    public static ServiceResult Success(string? message = null) => new(true, message);

    public static ServiceResult Failure(string message) => new(false, message);
}
