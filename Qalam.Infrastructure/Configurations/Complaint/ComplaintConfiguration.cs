using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Complaint;

namespace Qalam.Infrastructure.Configurations.Complaint;

public class ComplaintConfiguration : IEntityTypeConfiguration<Data.Entity.Complaint.Complaint>
{
    public void Configure(EntityTypeBuilder<Data.Entity.Complaint.Complaint> builder)
    {
        builder.ToTable("Complaints", "complaint");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.SubjectType);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => e.ComplainantUserId);
        builder.HasIndex(e => e.AssignedToUserId);
        builder.HasIndex(e => e.CourseScheduleId);
        builder.HasIndex(e => e.EnrollmentId);
        builder.HasIndex(e => e.PaymentId);
        builder.HasIndex(e => e.RefundId);
        builder.HasIndex(e => e.OpenSessionRequestId);
        builder.HasIndex(e => e.LegacySessionComplaintId).IsUnique();
        builder.HasIndex(e => new { e.SubjectType, e.Status, e.FiledAt });
        builder.HasIndex(e => new { e.CourseScheduleId, e.AffectedStudentId, e.Status });

        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.ResolutionNotes).HasMaxLength(4000);
        builder.Property(e => e.RespondentResponse).HasMaxLength(4000);
        builder.Property(e => e.ComplainantResponse).HasMaxLength(4000);

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.Complaint)
            .HasForeignKey(a => a.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Timeline)
            .WithOne(t => t.Complaint)
            .HasForeignKey(t => t.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
