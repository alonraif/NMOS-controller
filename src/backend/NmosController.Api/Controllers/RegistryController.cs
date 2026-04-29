using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Settings;
using NmosController.Contracts.Requests;
using NmosController.Contracts.Responses;

namespace NmosController.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/registry")]
public sealed class RegistryController(IRegistryService registryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<RegistrySettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var registry = await registryService.GetAsync(cancellationToken);
        return registry is null
            ? NotFound()
            : Ok(new ApiEnvelope<RegistrySettingsDto>(registry, DateTimeOffset.UtcNow));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiEnvelope<RegistrySettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateRegistrySettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await registryService.SaveAsync(
            new UpdateRegistrySettingsCommand(
                request.Name,
                request.BaseUrl,
                request.DiscoveryMode,
                request.MdnsQueryServiceType,
                request.MdnsResolveTimeoutMilliseconds,
                request.ConnectionBaseUrl,
                request.ConnectionBaseUrls,
                request.QueryApiVersion,
                request.ConnectionApiVersion,
                request.IsEnabled,
                request.InitialSetupCompleted),
            cancellationToken);

        return Ok(new ApiEnvelope<RegistrySettingsDto>(result, DateTimeOffset.UtcNow));
    }
}
