using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common.Enums;
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
        builder.HasIndex(e => e.SessionRequestId);
        builder.HasIndex(e => e.SessionOfferId);
        builder.HasIndex(e => e.LeaderStudentId);
        builder.HasIndex(e => new { e.Source, e.EnrollmentStatus });
        builder.HasIndex(e => new { e.CourseId, e.EnrollmentStatus });
        builder.HasIndex(e => new { e.EnrollmentStatus, e.PaymentDeadline });
        builder.HasIndex(e => e.PaidByUserId);
        builder.HasIndex(e => e.OwnerUserId);

        // Properties
        builder.Property(e => e.Source).IsRequired().HasDefaultValue(EnrollmentSource.CourseRequest);
        builder.Property(e => e.Kind).IsRequired();
        builder.Property(e => e.EnrollmentStatus).IsRequired();
        builder.Property(e => e.AmountDue).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(e => e.PreferredStartDate).HasColumnType("date");
        builder.Property(e => e.PreferredEndDate).HasColumnType("date");

        // Relationships — Course optional (Scenario 2 has no Course).
        // Cascade demoted to Restrict because Course is no longer the sole parent.
        builder.HasOne(e => e.Course)
               .WithMany(c => c.Enrollments)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EnrollmentRequest)
               .WithMany()
               .HasForeignKey(e => e.EnrollmentRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OpenSessionRequest)
               .WithMany(r => r.Enrollments)
               .HasForeignKey(e => e.SessionRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OpenSessionOffer)
               .WithMany()
               .HasForeignKey(e => e.SessionOfferId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByTeacher)
               .WithMany()
               .HasForeignKey(e => e.ApprovedByTeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LeaderStudent)
               .WithMany()
               .HasForeignKey(e => e.LeaderStudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PaidByUser)
               .WithMany()
               .HasForeignKey(e => e.PaidByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OwnerUser)
               .WithMany()
               .HasForeignKey(e => e.OwnerUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Participants)
               .WithOne(p => p.Enrollment)
               .HasForeignKey(p => p.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.CourseSchedules)
               .WithOne(cs => cs.Enrollment)
               .HasForeignKey(cs => cs.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.SelectedSessionSlots)
               .WithOne(s => s.Enrollment)
               .HasForeignKey(s => s.EnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
