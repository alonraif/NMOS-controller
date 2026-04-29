using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        await EnsureSchemaFixupsAsync(dbContext, cancellationToken);

        var existingRegistry = await dbContext.Registries
            .OrderByDescending(entity => entity.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingRegistry is null)
        {
            dbContext.Registries.Add(new RegistryConfigurationEntity
            {
                Id = Guid.NewGuid(),
                Name = options.Registry.Name,
                BaseUrl = options.Registry.BaseUrl,
                ConnectionBaseUrl = options.Registry.ConnectionBaseUrl,
                ConnectionBaseUrls = options.Registry.ConnectionBaseUrls,
                QueryApiVersion = options.Registry.QueryApiVersion,
                ConnectionApiVersion = options.Registry.ConnectionApiVersion,
                IsEnabled = options.Registry.IsEnabled,
                InitialSetupCompleted = false,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        else if (!existingRegistry.InitialSetupCompleted)
        {
            existingRegistry.Name = options.Registry.Name;
            existingRegistry.BaseUrl = options.Registry.BaseUrl;
            existingRegistry.ConnectionBaseUrl = options.Registry.ConnectionBaseUrl;
            existingRegistry.ConnectionBaseUrls = options.Registry.ConnectionBaseUrls;
            existingRegistry.QueryApiVersion = options.Registry.QueryApiVersion;
            existingRegistry.ConnectionApiVersion = options.Registry.ConnectionApiVersion;
            existingRegistry.IsEnabled = options.Registry.IsEnabled;
            existingRegistry.UpdatedAtUtc = DateTimeOffset.UtcNow;
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
                Description = "Connect Program Audio to the Audio Room destination.",
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

    private static async Task EnsureSchemaFixupsAsync(ControllerDbContext dbContext, CancellationToken cancellationToken)
    {
        var database = dbContext.Database;
        if (!database.IsNpgsql())
        {
            return;
        }

        await database.ExecuteSqlRawAsync(
            "ALTER TABLE IF EXISTS registry_configurations ADD COLUMN IF NOT EXISTS \"ConnectionBaseUrl\" character varying(1024);",
            [],
            cancellationToken);
        await database.ExecuteSqlRawAsync(
            "ALTER TABLE IF EXISTS registry_configurations ADD COLUMN IF NOT EXISTS \"ConnectionBaseUrls\" character varying(4096);",
            [],
            cancellationToken);
        await database.ExecuteSqlRawAsync(
            "ALTER TABLE IF EXISTS registry_configurations ADD COLUMN IF NOT EXISTS \"InitialSetupCompleted\" boolean NOT NULL DEFAULT FALSE;",
            [],
            cancellationToken);
    }
}
