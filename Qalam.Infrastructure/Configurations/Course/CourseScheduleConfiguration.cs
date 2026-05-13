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
        builder.HasIndex(e => e.EnrollmentId);
        builder.HasIndex(e => e.TeacherAvailabilityId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Status);

        // Properties
        builder.Property(e => e.Date).HasColumnType("date");
        builder.Property(e => e.DurationMinutes).IsRequired();

        // Relationships
        builder.HasOne(e => e.Enrollment)
               .WithMany(en => en.CourseSchedules)
               .HasForeignKey(e => e.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TeacherAvailability)
               .WithMany()
               .HasForeignKey(e => e.TeacherAvailabilityId)
               .OnDelete(DeleteBehavior.Restrict);

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
