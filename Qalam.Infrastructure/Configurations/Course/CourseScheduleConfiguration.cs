using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseScheduleConfiguration : IEntityTypeConfiguration<CourseSchedule>
{
    public void Configure(EntityTypeBuilder<CourseSchedule> builder)
    {
        builder.ToTable("CourseSchedules", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.CourseEnrollmentId);
        builder.HasIndex(e => e.CourseGroupEnrollmentId);
        builder.HasIndex(e => e.TeacherAvailabilityId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Status);
        
        // Properties
        builder.Property(e => e.Date).HasColumnType("date");
        builder.Property(e => e.DurationMinutes).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CourseSchedule_EnrollmentLink",
            "(([CourseEnrollmentId] IS NOT NULL AND [CourseGroupEnrollmentId] IS NULL) OR ([CourseEnrollmentId] IS NULL AND [CourseGroupEnrollmentId] IS NOT NULL))"
        ));
        
        // Relationships (changed from cascade to restrict)
        builder.HasOne(e => e.CourseEnrollment)
               .WithMany(ce => ce.CourseSchedules)
               .HasForeignKey(e => e.CourseEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CourseGroupEnrollment)
               .WithMany(cge => cge.CourseSchedules)
               .HasForeignKey(e => e.CourseGroupEnrollmentId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.TeacherAvailability)
               .WithMany()
               .HasForeignKey(e => e.TeacherAvailabilityId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasOne(e => e.TeachingMode)
               .WithMany()
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Location)
               .WithMany()
               .HasForeignKey(e => e.LocationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
