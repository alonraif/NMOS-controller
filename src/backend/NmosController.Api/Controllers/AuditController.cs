using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Contracts.Responses;

namespace NmosController.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyCollection<AuditEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var auditEntries = await auditService.GetRecentAsync(limit, cancellationToken);
        return Ok(new ApiEnvelope<IReadOnlyCollection<AuditEntryDto>>(auditEntries, DateTimeOffset.UtcNow));
    }
}
