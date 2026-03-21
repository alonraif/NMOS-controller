using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionEntityConfiguration : IEntityTypeConfiguration<UserSessionEntity>
{
    public void Configure(EntityTypeBuilder<UserSessionEntity> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RemoteAddress).HasMaxLength(128);
        builder.Property(x => x.UserAgent).HasMaxLength(2048);
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.StartedAtUtc).IsRequired();
    }
}
