using NmosController.Domain.Enums;

namespace NmosController.Application.Sessions;

public sealed record EndUserSessionCommand(
    SessionState State);
