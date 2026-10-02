using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Messaging;

namespace Qalam.Infrastructure.Configurations.Messaging;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications", "messaging");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(60);
        builder.Property(e => e.TitleAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TitleEn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.BodyAr).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.BodyEn).IsRequired().HasMaxLength(1000);
        builder.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
