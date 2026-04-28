using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Configurations;

internal sealed class RegistryConfigurationEntityConfiguration : IEntityTypeConfiguration<RegistryConfigurationEntity>
{
    public void Configure(EntityTypeBuilder<RegistryConfigurationEntity> builder)
    {
        builder.ToTable("registry_configurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BaseUrl).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ConnectionBaseUrl).HasMaxLength(1024);
        builder.Property(x => x.ConnectionBaseUrls).HasMaxLength(4096);
        builder.Property(x => x.QueryApiVersion).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ConnectionApiVersion).HasMaxLength(32).IsRequired();
        builder.Property(x => x.InitialSetupCompleted).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
