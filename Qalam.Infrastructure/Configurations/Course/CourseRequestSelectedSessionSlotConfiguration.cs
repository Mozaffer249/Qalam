using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseRequestSelectedSessionSlotConfiguration : IEntityTypeConfiguration<CourseRequestSelectedSessionSlot>
{
    public void Configure(EntityTypeBuilder<CourseRequestSelectedSessionSlot> builder)
    {
        builder.ToTable("CourseRequestSelectedSessionSlots", "course");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionDate).HasColumnType("date");

        builder.HasIndex(e => e.CourseEnrollmentRequestId);
        builder.HasIndex(e => e.TeacherAvailabilityId);
        builder.HasIndex(e => new { e.CourseEnrollmentRequestId, e.SessionNumber }).IsUnique();

        builder.HasOne(e => e.CourseEnrollmentRequest)
            .WithMany(c => c.SelectedSessionSlots)
            .HasForeignKey(e => e.CourseEnrollmentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TeacherAvailability)
            .WithMany()
            .HasForeignKey(e => e.TeacherAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
