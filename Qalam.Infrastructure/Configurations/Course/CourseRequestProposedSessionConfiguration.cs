using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseRequestProposedSessionConfiguration : IEntityTypeConfiguration<CourseRequestProposedSession>
{
    public void Configure(EntityTypeBuilder<CourseRequestProposedSession> builder)
    {
        builder.ToTable("CourseRequestProposedSessions", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseEnrollmentRequestId);
        builder.HasIndex(e => new { e.CourseEnrollmentRequestId, e.SessionNumber }).IsUnique();

        builder.Property(e => e.Title).HasMaxLength(150);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.CourseEnrollmentRequest)
               .WithMany(r => r.ProposedSessions)
               .HasForeignKey(e => e.CourseEnrollmentRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
