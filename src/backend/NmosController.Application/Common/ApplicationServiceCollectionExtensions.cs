using Microsoft.Extensions.DependencyInjection;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Services;
using NmosController.Domain.Services;

namespace NmosController.Application.Common;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ConnectionCompatibilityEvaluator>();
        services.AddScoped<ITopologyService, TopologyService>();
        services.AddScoped<IRoutingService, RoutingService>();
        services.AddScoped<IRegistryService, RegistryService>();
        services.AddScoped<IPresetService, PresetService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        return services;
    }
}
