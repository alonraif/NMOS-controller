using Microsoft.AspNetCore.Http;

namespace NmosController.Infrastructure.Observability;

internal sealed class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-ID";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault()
                            ?? httpContextAccessor.HttpContext?.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(HeaderName))
        {
            request.Headers.Add(HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
