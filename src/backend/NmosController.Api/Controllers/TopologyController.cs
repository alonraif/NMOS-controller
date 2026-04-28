using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Topology;
using NmosController.Contracts.Responses;

namespace NmosController.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class TopologyController(ITopologyService topologyService) : ControllerBase
{
    public sealed record ReceiverGroupingDiagnosticDto(
        string ReceiverId,
        string ReceiverLabel,
        string DeviceId,
        string SignalType,
        string RoutingDestinationId,
        string RoutingDestinationLabel);

    [HttpGet("topology")]
    [ProducesResponseType(typeof(ApiEnvelope<TopologyGraphDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopologyAsync([FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        var topology = await topologyService.GetTopologyAsync(refresh, cancellationToken);
        return Ok(new ApiEnvelope<TopologyGraphDto>(topology, DateTimeOffset.UtcNow));
    }

    [HttpGet("senders")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<NmosSenderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSendersAsync([FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        var senders = await topologyService.GetSendersAsync(refresh, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<NmosSenderDto>>(senders, DateTimeOffset.UtcNow));
    }

    [HttpGet("receivers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<NmosReceiverDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceiversAsync([FromQuery] bool refresh = false, CancellationToken cancellationToken = default)
    {
        var receivers = await topologyService.GetReceiversAsync(refresh, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<NmosReceiverDto>>(receivers, DateTimeOffset.UtcNow));
    }

    [HttpGet("topology/diagnostics/receiver-groups")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<ReceiverGroupingDiagnosticDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceiverGroupsAsync([FromQuery] bool refresh = true, CancellationToken cancellationToken = default)
    {
        var receivers = await topologyService.GetReceiversAsync(refresh, cancellationToken);
        var payload = receivers
            .Select(
                x => new ReceiverGroupingDiagnosticDto(
                    x.Id,
                    x.Label,
                    x.DeviceId,
                    x.SignalType,
                    x.RoutingDestinationId,
                    x.RoutingDestinationLabel))
            .OrderBy(x => x.ReceiverLabel)
            .ToArray();

        return Ok(new ApiEnvelope<IReadOnlyCollection<ReceiverGroupingDiagnosticDto>>(payload, DateTimeOffset.UtcNow));
    }

    [HttpGet("resources/{resourceId}")]
    [ProducesResponseType(typeof(ApiEnvelope<ResourceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResourceAsync(string resourceId, CancellationToken cancellationToken)
    {
        var resource = await topologyService.GetResourceAsync(resourceId, cancellationToken);
        return resource is null
            ? NotFound()
            : Ok(new ApiEnvelope<ResourceDetailDto>(resource, DateTimeOffset.UtcNow));
    }
}
