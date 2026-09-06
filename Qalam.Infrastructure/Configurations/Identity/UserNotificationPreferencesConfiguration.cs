using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Configurations.Identity;

public class UserNotificationPreferencesConfiguration : IEntityTypeConfiguration<UserNotificationPreferences>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreferences> builder)
    {
        builder.ToTable("UserNotificationPreferences", "security");
        builder.HasKey(e => e.UserId);
        builder.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<UserNotificationPreferences>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
