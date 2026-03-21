using NmosController.Application.Common;
using NmosController.Application.Routing;

namespace NmosController.Application.Abstractions.Services;

public interface IRoutingService
{
    Task<RouteValidationResultDto> ValidateAsync(RouteValidationCommand command, CancellationToken cancellationToken);
    Task<ServiceResult> ConnectAsync(RouteConnectCommand command, CancellationToken cancellationToken);
    Task<ServiceResult> DisconnectAsync(RouteDisconnectCommand command, CancellationToken cancellationToken);
}
