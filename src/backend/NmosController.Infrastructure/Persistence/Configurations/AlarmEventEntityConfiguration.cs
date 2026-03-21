using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Configurations;

internal sealed class AlarmEventEntityConfiguration : IEntityTypeConfiguration<AlarmEventEntity>
{
    public void Configure(EntityTypeBuilder<AlarmEventEntity> builder)
    {
        builder.ToTable("alarm_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(256);
        builder.Property(x => x.RaisedAtUtc).IsRequired();
    }
}
