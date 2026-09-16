using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Complaint;

namespace Qalam.Infrastructure.Configurations.Complaint;

public class ComplaintAttachmentConfiguration : IEntityTypeConfiguration<ComplaintAttachment>
{
    public void Configure(EntityTypeBuilder<ComplaintAttachment> builder)
    {
        builder.ToTable("ComplaintAttachments", "complaint");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ComplaintId);
        builder.Property(e => e.FileUrl).HasMaxLength(1000);
        builder.Property(e => e.FileName).HasMaxLength(255);
        builder.Property(e => e.ContentType).HasMaxLength(128);
    }
}
