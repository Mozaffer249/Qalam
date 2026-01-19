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
        builder.HasIndex(e => e.TeacherAvailabilityId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Status);
        
        // Properties
        builder.Property(e => e.Date).HasColumnType("date");
        
        // Relationships (changed from cascade to restrict)
        builder.HasOne(e => e.CourseEnrollment)
               .WithMany(ce => ce.CourseSchedules)
               .HasForeignKey(e => e.CourseEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
        
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
