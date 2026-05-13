using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments", "course");

        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => e.ApprovedByTeacherId);
        builder.HasIndex(e => e.EnrollmentRequestId);
        builder.HasIndex(e => e.LeaderStudentId);
        builder.HasIndex(e => new { e.CourseId, e.EnrollmentStatus });
        builder.HasIndex(e => new { e.EnrollmentStatus, e.PaymentDeadline });

        // Properties
        builder.Property(e => e.Kind).IsRequired();
        builder.Property(e => e.EnrollmentStatus).IsRequired();

        // Relationships
        builder.HasOne(e => e.Course)
               .WithMany(c => c.Enrollments)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EnrollmentRequest)
               .WithMany()
               .HasForeignKey(e => e.EnrollmentRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByTeacher)
               .WithMany()
               .HasForeignKey(e => e.ApprovedByTeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LeaderStudent)
               .WithMany()
               .HasForeignKey(e => e.LeaderStudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Participants)
               .WithOne(p => p.Enrollment)
               .HasForeignKey(p => p.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.CourseSchedules)
               .WithOne(cs => cs.Enrollment)
               .HasForeignKey(cs => cs.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
