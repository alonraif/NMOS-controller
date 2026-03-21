using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryEntityConfiguration : IEntityTypeConfiguration<AuditEntryEntity>
{
    public void Configure(EntityTypeBuilder<AuditEntryEntity> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(256);
        builder.Property(x => x.ResourceType).HasMaxLength(64);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}
