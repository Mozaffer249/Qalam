using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseGroupEnrollmentMemberConfiguration : IEntityTypeConfiguration<CourseGroupEnrollmentMember>
{
    public void Configure(EntityTypeBuilder<CourseGroupEnrollmentMember> builder)
    {
        builder.ToTable("CourseGroupEnrollmentMembers", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseGroupEnrollmentId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.PaymentStatus);
        builder.HasIndex(e => new { e.CourseGroupEnrollmentId, e.StudentId }).IsUnique();

        builder.HasOne(e => e.CourseGroupEnrollment)
               .WithMany(g => g.Members)
               .HasForeignKey(e => e.CourseGroupEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.GroupEnrollmentMemberPayments)
               .WithOne(p => p.CourseGroupEnrollmentMember)
               .HasForeignKey(p => p.CourseGroupEnrollmentMemberId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
