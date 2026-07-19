using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class EnrollmentSelectedSessionSlotConfiguration
    : IEntityTypeConfiguration<EnrollmentSelectedSessionSlot>
{
    public void Configure(EntityTypeBuilder<EnrollmentSelectedSessionSlot> builder)
    {
        builder.ToTable("EnrollmentSelectedSessionSlots", "course");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SessionDate).HasColumnType("date");

        builder.HasIndex(e => e.EnrollmentId);
        builder.HasIndex(e => e.TeacherAvailabilityId);
        builder.HasIndex(e => new { e.EnrollmentId, e.SessionNumber }).IsUnique();

        builder.HasOne(e => e.Enrollment)
            .WithMany(c => c.SelectedSessionSlots)
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TeacherAvailability)
            .WithMany()
            .HasForeignKey(e => e.TeacherAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
