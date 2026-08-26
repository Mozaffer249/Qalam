using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Student;

namespace Qalam.Infrastructure.Configurations.Student;

public class StudentFreeTrialConsumptionConfiguration : IEntityTypeConfiguration<StudentFreeTrialConsumption>
{
    public void Configure(EntityTypeBuilder<StudentFreeTrialConsumption> builder)
    {
        builder.ToTable("StudentFreeTrialConsumptions", "student");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.StudentId);
        builder.HasIndex(c => c.EnrollmentId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => new { c.StudentId, c.Status });

        builder.Property(c => c.Source).IsRequired();
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.CancelReason).HasMaxLength(500);
        builder.Property(c => c.ReservedAt).IsRequired();

        builder.HasOne(c => c.Student)
            .WithMany()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Enrollment)
            .WithMany()
            .HasForeignKey(c => c.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CourseSchedule)
            .WithMany()
            .HasForeignKey(c => c.CourseScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
