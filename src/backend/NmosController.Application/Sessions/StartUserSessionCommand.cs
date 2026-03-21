namespace NmosController.Application.Sessions;

public sealed record StartUserSessionCommand(
    string UserName,
    string DisplayName,
    string? RemoteAddress,
    string? UserAgent);
