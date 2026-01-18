using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Identity;
using StudentEntity = Qalam.Data.Entity.Student.Student;
using GuardianEntity = Qalam.Data.Entity.Student.Guardian;

namespace Qalam.Infrastructure.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // One User can have one Student profile (optional)
        builder.HasOne(u => u.StudentProfile)
               .WithOne(s => s.User)
               .HasForeignKey<StudentEntity>(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // One User can have one Guardian profile (optional)
        builder.HasOne(u => u.GuardianProfile)
               .WithOne(g => g.User)
               .HasForeignKey<GuardianEntity>(g => g.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
