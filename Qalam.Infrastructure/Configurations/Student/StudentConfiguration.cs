using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentEntity = Qalam.Data.Entity.Student.Student;

namespace Qalam.Infrastructure.Configurations.Student;

public class StudentConfiguration : IEntityTypeConfiguration<StudentEntity>
{
    public void Configure(EntityTypeBuilder<StudentEntity> builder)
    {
        builder.ToTable("Students", "student", t =>
        {
            // Minor must have a guardian
            t.HasCheckConstraint(
                "CK_Students_Minor_RequiresGuardian",
                "([IsMinor] = 0) OR ([IsMinor] = 1 AND [GuardianId] IS NOT NULL)"
            );
        });

        builder.HasKey(s => s.Id);

        // Indexes
        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => s.GuardianId);
        builder.HasIndex(s => s.IsMinor);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => new { s.DomainId, s.CurriculumId, s.LevelId, s.GradeId });

        // Properties
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.IsMinor).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.Bio).HasMaxLength(500);
        builder.Property(s => s.DateOfBirth).HasColumnType("date");

        // Relationships

        // User (One-to-One) — Student.UserId required
        builder.HasOne(s => s.User)
               .WithOne(u => u.StudentProfile)
               .HasForeignKey<StudentEntity>(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Guardian (Many Students -> One Guardian)
        builder.HasOne(s => s.Guardian)
               .WithMany(g => g.Students)
               .HasForeignKey(s => s.GuardianId)
               .OnDelete(DeleteBehavior.Restrict);

        // Education references
        builder.HasOne(s => s.Domain)
               .WithMany()
               .HasForeignKey(s => s.DomainId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Curriculum)
               .WithMany()
               .HasForeignKey(s => s.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Level)
               .WithMany()
               .HasForeignKey(s => s.LevelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Grade)
               .WithMany()
               .HasForeignKey(s => s.GradeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
