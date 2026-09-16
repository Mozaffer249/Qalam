using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Complaint;

namespace Qalam.Infrastructure.Configurations.Complaint;

public class ComplaintTimelineEntryConfiguration : IEntityTypeConfiguration<ComplaintTimelineEntry>
{
    public void Configure(EntityTypeBuilder<ComplaintTimelineEntry> builder)
    {
        builder.ToTable("ComplaintTimelineEntries", "complaint");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ComplaintId);
        builder.HasIndex(e => new { e.ComplaintId, e.OccurredAt });
        builder.Property(e => e.ActorRole).HasMaxLength(64);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.Property(e => e.PayloadJson).HasMaxLength(8000);
    }
}
