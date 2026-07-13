using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class SessionHomeworkFileLinkConfiguration : IEntityTypeConfiguration<SessionHomeworkFileLink>
{
    public void Configure(EntityTypeBuilder<SessionHomeworkFileLink> builder)
    {
        builder.ToTable("SessionHomeworkFileLinks", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.SessionHomeworkAssignmentId, e.ContentItemId }).IsUnique();

        builder.HasOne(e => e.Assignment)
            .WithMany(a => a.FileLinks)
            .HasForeignKey(e => e.SessionHomeworkAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ContentItem)
            .WithMany()
            .HasForeignKey(e => e.ContentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
