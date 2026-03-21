using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NmosController.Api.Extensions;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Routing;
using NmosController.Contracts.Requests;
using NmosController.Contracts.Responses;

namespace NmosController.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/routing")]
public sealed class RoutingController(IRoutingService routingService) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiEnvelope<RouteValidationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateAsync([FromBody] RouteValidationRequest request, CancellationToken cancellationToken)
    {
        var result = await routingService.ValidateAsync(
            new RouteValidationCommand(
                request.ReceiverId,
                request.SenderId,
                request.ToActivationRequest()),
            cancellationToken);

        return Ok(new ApiEnvelope<RouteValidationResultDto>(result, DateTimeOffset.UtcNow));
    }

    [HttpPost("receivers/{receiverId}/connect")]
    [ProducesResponseType(typeof(ApiEnvelope<RouteOperationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConnectAsync(string receiverId, [FromBody] ConnectReceiverRequest request, CancellationToken cancellationToken)
    {
        var result = await routingService.ConnectAsync(
            new RouteConnectCommand(receiverId, request.SenderId, request.RequestedBy, request.ToActivationRequest()),
            cancellationToken);

        var response = new ApiEnvelope<RouteOperationResponse>(new RouteOperationResponse(result.Succeeded, result.Message), DateTimeOffset.UtcNow);
        return result.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpPost("receivers/{receiverId}/disconnect")]
    [ProducesResponseType(typeof(ApiEnvelope<RouteOperationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DisconnectAsync(string receiverId, [FromBody] DisconnectReceiverRequest request, CancellationToken cancellationToken)
    {
        var result = await routingService.DisconnectAsync(
            new RouteDisconnectCommand(receiverId, request.RequestedBy, request.ToActivationRequest()),
            cancellationToken);

        var response = new ApiEnvelope<RouteOperationResponse>(new RouteOperationResponse(result.Succeeded, result.Message), DateTimeOffset.UtcNow);
        return result.Succeeded ? Ok(response) : BadRequest(response);
    }
}
