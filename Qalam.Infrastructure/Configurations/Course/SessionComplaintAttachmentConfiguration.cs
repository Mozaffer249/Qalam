using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class SessionComplaintAttachmentConfiguration : IEntityTypeConfiguration<SessionComplaintAttachment>
{
    public void Configure(EntityTypeBuilder<SessionComplaintAttachment> builder)
    {
        builder.ToTable("SessionComplaintAttachments", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.ComplaintId);

        builder.Property(e => e.FileUrl).HasMaxLength(2000);
        builder.Property(e => e.FileName).HasMaxLength(500);
        builder.Property(e => e.ContentType).HasMaxLength(200);

        builder.HasOne(e => e.Complaint)
            .WithMany(c => c.Attachments)
            .HasForeignKey(e => e.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
