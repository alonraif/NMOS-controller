using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace NmosController.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(context, ex, HttpStatusCode.BadRequest, logger);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex, HttpStatusCode.InternalServerError, logger);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception, HttpStatusCode statusCode, ILogger logger)
    {
        logger.LogError(exception, "Request failed for {Path}", context.Request.Path);

        var problem = new ProblemDetails
        {
            Title = statusCode == HttpStatusCode.InternalServerError ? "Unhandled server error" : "Request failed",
            Detail = exception.Message,
            Status = (int)statusCode,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
