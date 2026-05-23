using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class SessionRequestAttachmentConfiguration : IEntityTypeConfiguration<OpenSessionRequestAttachment>
{
    public void Configure(EntityTypeBuilder<OpenSessionRequestAttachment> builder)
    {
        builder.ToTable("SessionRequestAttachments", "sr");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.SessionRequestId);

        builder.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.StorageKey).IsRequired().HasMaxLength(1024);
        builder.Property(e => e.PublicUrl).HasMaxLength(1024);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
    }
}
