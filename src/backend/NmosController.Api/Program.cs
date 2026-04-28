using Asp.Versioning.ApiExplorer;
using System.Text.Json.Serialization;
using NmosController.Api.Middleware;
using NmosController.Application.Common;
using NmosController.Infrastructure;
using NmosController.Infrastructure.Configuration;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId();
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddHealthChecks();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var corsOrigins = builder.Configuration.GetSection($"{NmosControllerOptions.SectionName}:Cors")
    .Get<CorsOptions>()?.AllowedOrigins
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["*"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "nmos-controller-ui",
        policy =>
        {
            if (corsOrigins.Any(origin => origin == "*"))
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                return;
            }

            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        });
});
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddPrometheusExporter();
    });

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("nmos-controller-ui");
app.UseRouting();
app.MapGet(
        "/",
        () => Results.Ok(
            new
            {
                service = "NmosController.Api",
                status = "ok",
                utc = DateTimeOffset.UtcNow
            }))
    .WithName("Root");

app.MapGet(
        "/health",
        () => Results.Ok(
            new
            {
                status = "Healthy",
                utc = DateTimeOffset.UtcNow
            }))
    .WithName("Health");

app.MapGet(
        "/ready",
        () => Results.Ok(
            new
            {
                status = "Ready",
                utc = DateTimeOffset.UtcNow
            }))
    .WithName("Readiness");

app.MapPrometheusScrapingEndpoint("/metrics");
app.MapControllers();

app.Run();

public partial class Program;
