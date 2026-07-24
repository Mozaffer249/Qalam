using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class SessionAttendanceConfiguration : IEntityTypeConfiguration<SessionAttendance>
{
    public void Configure(EntityTypeBuilder<SessionAttendance> builder)
    {
        builder.ToTable("SessionAttendances", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.CourseScheduleId, e.StudentId }).IsUnique();
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.Rating).HasPrecision(3, 1);
        builder.Property(e => e.Note).HasMaxLength(2000);

        builder.HasOne(e => e.CourseSchedule)
            .WithMany(s => s.Attendances)
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
