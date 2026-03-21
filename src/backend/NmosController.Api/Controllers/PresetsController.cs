using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NmosController.Api.Extensions;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Presets;
using NmosController.Contracts.Requests;
using NmosController.Contracts.Responses;
using NmosController.Domain.ValueObjects;

namespace NmosController.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/presets")]
public sealed class PresetsController(IPresetService presetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<PresetSalvoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var presets = await presetService.GetAllAsync(cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<PresetSalvoDto>>(presets, DateTimeOffset.UtcNow));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<PresetSalvoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var preset = await presetService.GetByIdAsync(id, cancellationToken);
        return preset is null
            ? NotFound()
            : Ok(new ApiEnvelope<PresetSalvoDto>(preset, DateTimeOffset.UtcNow));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiEnvelope<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveAsync([FromBody] UpsertPresetSalvoRequest request, CancellationToken cancellationToken)
    {
        var routes = request.Routes.Select(
            x => new PresetRoute(
                x.ReceiverId,
                x.SenderId,
                x.ToActivationRequest())).ToArray();

        var presetId = await presetService.SaveAsync(
            new UpsertPresetSalvoCommand(request.Id, request.Name, request.Description, routes),
            cancellationToken);

        return Ok(new ApiEnvelope<Guid>(presetId, DateTimeOffset.UtcNow));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<RouteOperationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await presetService.DeleteAsync(id, cancellationToken);
        var response = new ApiEnvelope<RouteOperationResponse>(new RouteOperationResponse(result.Succeeded, result.Message), DateTimeOffset.UtcNow);
        return result.Succeeded ? Ok(response) : NotFound(response);
    }

    [HttpPost("{id:guid}/execute")]
    [ProducesResponseType(typeof(ApiEnvelope<RouteOperationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecuteAsync(Guid id, [FromBody] ExecutePresetRequest request, CancellationToken cancellationToken)
    {
        var activation = request.ActivationMode.HasValue
            ? request.ToActivationRequest()
            : null;

        var result = await presetService.ExecuteAsync(
            new ExecutePresetSalvoCommand(id, request.RequestedBy, activation),
            cancellationToken);

        var response = new ApiEnvelope<RouteOperationResponse>(new RouteOperationResponse(result.Succeeded, result.Message), DateTimeOffset.UtcNow);
        return result.Succeeded ? Ok(response) : BadRequest(response);
    }
}
