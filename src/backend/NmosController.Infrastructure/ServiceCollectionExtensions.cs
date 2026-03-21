using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Nmos.Clients;
using NmosController.Infrastructure.Nmos.Mock;
using NmosController.Infrastructure.Nmos.Switching;
using NmosController.Infrastructure.Observability;
using NmosController.Infrastructure.Persistence;
using NmosController.Infrastructure.Persistence.Repositories;

namespace NmosController.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NmosControllerOptions>(configuration.GetSection(NmosControllerOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.AddDbContext<ControllerDbContext>((serviceProvider, options) =>
        {
            var controllerOptions = serviceProvider.GetRequiredService<IOptions<NmosControllerOptions>>().Value;
            options.UseNpgsql(
                controllerOptions.Postgres.ConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ControllerDbContext).Assembly.FullName));
        });

        services.AddScoped<IRegistrySettingsResolver, RegistrySettingsResolver>();
        services.AddHostedService<DatabaseInitializationHostedService>();

        services.AddScoped<IRegistryRepository, RegistryRepository>();
        services.AddScoped<IPresetSalvoRepository, PresetSalvoRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAlarmRepository, AlarmRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();

        services.AddSingleton<MockNmosFixtureStore>();
        services.AddScoped<MockNmosQueryClient>();
        services.AddScoped<MockNmosConnectionClient>();

        services.AddHttpClient<NmosQueryApiClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<NmosControllerOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.Http.TimeoutSeconds);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddStandardResilienceHandler();

        services.AddHttpClient<NmosConnectionApiClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<NmosControllerOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.Http.TimeoutSeconds);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddStandardResilienceHandler();

        services.AddScoped<SwitchingNmosQueryClient>();
        services.AddScoped<SwitchingNmosConnectionClient>();
        services.AddScoped<INmosQueryClient>(serviceProvider => serviceProvider.GetRequiredService<SwitchingNmosQueryClient>());
        services.AddScoped<INmosConnectionClient>(serviceProvider => serviceProvider.GetRequiredService<SwitchingNmosConnectionClient>());

        return services;
    }
}
