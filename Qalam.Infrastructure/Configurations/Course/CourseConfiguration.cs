using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseConfiguration : IEntityTypeConfiguration<Qalam.Data.Entity.Course.Course>
{
    public void Configure(EntityTypeBuilder<Qalam.Data.Entity.Course.Course> builder)
    {
        builder.ToTable("Courses", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.TeacherSubjectId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.IsActive });
        builder.HasIndex(e => new { e.TeachingModeId, e.SessionTypeId });
        
        // Properties
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
        
        // Check Constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Course_SessionsCount",
            "([IsFlexible] = 1) OR ([SessionsCount] IS NOT NULL AND [SessionsCount] > 0)"
        ));
        
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Course_SessionDuration",
            "([IsFlexible] = 1) OR ([SessionDurationMinutes] IS NOT NULL AND [SessionDurationMinutes] > 0)"
        ));
        
        // Relationships
        builder.HasOne(e => e.Teacher)
               .WithMany(t => t.Courses)
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.TeacherSubject)
               .WithMany()
               .HasForeignKey(e => e.TeacherSubjectId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.TeachingMode)
               .WithMany()
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.SessionType)
               .WithMany()
               .HasForeignKey(e => e.SessionTypeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        // Collections
        builder.HasMany(e => e.CourseSessions)
               .WithOne(cs => cs.Course)
               .HasForeignKey(cs => cs.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.CourseEnrollmentRequests)
               .WithOne(cer => cer.Course)
               .HasForeignKey(cer => cer.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.CourseEnrollments)
               .WithOne(ce => ce.Course)
               .HasForeignKey(ce => ce.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
