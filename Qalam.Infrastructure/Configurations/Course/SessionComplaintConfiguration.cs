using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class SessionComplaintConfiguration : IEntityTypeConfiguration<SessionComplaint>
{
    public void Configure(EntityTypeBuilder<SessionComplaint> builder)
    {
        builder.ToTable("SessionComplaints", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseScheduleId);
        builder.HasIndex(e => e.EnrollmentId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.CourseScheduleId, e.StudentId, e.Status });

        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.ResolutionNotes).HasMaxLength(4000);
        builder.Property(e => e.TeacherResponse).HasMaxLength(4000);

        builder.HasOne(e => e.CourseSchedule)
            .WithMany(s => s.Complaints)
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Enrollment)
            .WithMany()
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
