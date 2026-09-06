using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Messaging;

namespace Qalam.Infrastructure.Configurations.Messaging;

public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("UserDeviceTokens", "messaging");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Token).IsRequired().HasMaxLength(512);
        builder.Property(e => e.Platform).IsRequired().HasMaxLength(20);
        builder.Property(e => e.AppVersion).HasMaxLength(50);
        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
