using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseGroupEnrollmentConfiguration : IEntityTypeConfiguration<CourseGroupEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseGroupEnrollment> builder)
    {
        builder.ToTable("CourseGroupEnrollments", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => e.EnrollmentRequestId).IsUnique();
        builder.HasIndex(e => e.LeaderStudentId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Course)
               .WithMany(c => c.CourseGroupEnrollments)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EnrollmentRequest)
               .WithMany()
               .HasForeignKey(e => e.EnrollmentRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LeaderStudent)
               .WithMany()
               .HasForeignKey(e => e.LeaderStudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Members)
               .WithOne(m => m.CourseGroupEnrollment)
               .HasForeignKey(m => m.CourseGroupEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.CourseSchedules)
               .WithOne(cs => cs.CourseGroupEnrollment)
               .HasForeignKey(cs => cs.CourseGroupEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
