using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Json;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence;

internal sealed class DatabaseInitializationHostedService(
    IServiceProvider serviceProvider,
    IOptions<NmosControllerOptions> options,
    ILogger<DatabaseInitializationHostedService> logger) : IHostedService
{
    private const int MaxAttempts = 10;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ControllerDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                await SeedAsync(dbContext, options.Value, cancellationToken);
                logger.LogInformation("Controller database is ready.");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(ex, "Database initialization attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        throw new InvalidOperationException("Controller database could not be initialized after repeated attempts.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedAsync(ControllerDbContext dbContext, NmosControllerOptions options, CancellationToken cancellationToken)
    {
        if (!await dbContext.Registries.AnyAsync(cancellationToken))
        {
            dbContext.Registries.Add(new RegistryConfigurationEntity
            {
                Id = Guid.NewGuid(),
                Name = options.Registry.Name,
                BaseUrl = options.Registry.BaseUrl,
                QueryApiVersion = options.Registry.QueryApiVersion,
                ConnectionApiVersion = options.Registry.ConnectionApiVersion,
                Mode = options.Mode,
                IsEnabled = options.Registry.IsEnabled,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (!await dbContext.Presets.AnyAsync(cancellationToken))
        {
            var demoRoutes = new[]
            {
                new PresetRoute(
                    "receiver-dest-audio-room-audio",
                    "sender-audio-program-b",
                    ActivationRequest.Immediate(DateTimeOffset.UtcNow))
            };

            dbContext.Presets.Add(new PresetSalvoEntity
            {
                Id = Guid.NewGuid(),
                Name = "Demo Audio Route",
                Description = "Connect Program Audio to the Audio Room destination in mock lab mode.",
                RoutesJson = JsonSerializer.Serialize(demoRoutes, NmosJsonSerializer.Default),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (!await dbContext.AuditEntries.AnyAsync(cancellationToken))
        {
            dbContext.AuditEntries.Add(new AuditEntryEntity
            {
                Id = Guid.NewGuid(),
                ActionType = AuditActionType.SettingsChanged,
                Actor = "system",
                Summary = "Initial controller configuration seeded.",
                ResourceType = "Registry",
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
