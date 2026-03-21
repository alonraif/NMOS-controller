using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Configurations;

internal sealed class PresetSalvoEntityConfiguration : IEntityTypeConfiguration<PresetSalvoEntity>
{
    public void Configure(EntityTypeBuilder<PresetSalvoEntity> builder)
    {
        builder.ToTable("preset_salvos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.RoutesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
