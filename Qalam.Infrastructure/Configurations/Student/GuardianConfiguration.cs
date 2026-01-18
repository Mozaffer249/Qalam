using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Student;

namespace Qalam.Infrastructure.Configurations.Student;

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("Guardians", "student");

        builder.HasKey(g => g.Id);

        // Indexes
        builder.HasIndex(g => g.UserId)
               .IsUnique()
               .HasFilter("[UserId] IS NOT NULL"); // Important for nullable UserId

        builder.HasIndex(g => g.Phone);
        builder.HasIndex(g => g.Email);
        builder.HasIndex(g => g.IsActive);

        // Properties
        builder.Property(g => g.FullName).HasMaxLength(200);
        builder.Property(g => g.Phone).HasMaxLength(20);
        builder.Property(g => g.Email).HasMaxLength(256);
        builder.Property(g => g.IsActive).HasDefaultValue(true);

        // Relationship with User (optional One-to-One)
        builder.HasOne(g => g.User)
               .WithOne(u => u.GuardianProfile)
               .HasForeignKey<Guardian>(g => g.UserId)
               .OnDelete(DeleteBehavior.Restrict);

    }
}
