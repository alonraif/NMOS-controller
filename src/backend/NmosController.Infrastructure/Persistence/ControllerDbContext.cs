using Microsoft.EntityFrameworkCore;
using NmosController.Infrastructure.Persistence.Configurations;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence;

public sealed class ControllerDbContext(DbContextOptions<ControllerDbContext> options) : DbContext(options)
{
    public DbSet<RegistryConfigurationEntity> Registries => Set<RegistryConfigurationEntity>();
    public DbSet<PresetSalvoEntity> Presets => Set<PresetSalvoEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();
    public DbSet<AlarmEventEntity> AlarmEvents => Set<AlarmEventEntity>();
    public DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RegistryConfigurationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PresetSalvoEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AuditEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AlarmEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionEntityConfiguration());
    }
}
