using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherReviewConfiguration : IEntityTypeConfiguration<TeacherReview>
{
    public void Configure(EntityTypeBuilder<TeacherReview> builder)
    {
        builder.ToTable("TeacherReviews");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Feedback).HasMaxLength(600);

        // One student review per session when SessionId is set (CourseSchedule.Id).
        builder.HasIndex(e => new { e.StudentId, e.SessionId })
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL");

        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.SessionId);

        builder.HasOne(e => e.Teacher)
            .WithMany(t => t.TeacherReviews)
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
