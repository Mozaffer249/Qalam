using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class SessionContentLinkConfiguration : IEntityTypeConfiguration<SessionContentLink>
{
    public void Configure(EntityTypeBuilder<SessionContentLink> builder)
    {
        builder.ToTable("SessionContentLinks", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.CourseScheduleId, e.ContentItemId }).IsUnique();

        builder.HasOne(e => e.CourseSchedule)
            .WithMany()
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ContentItem)
            .WithMany(e => e.SessionLinks)
            .HasForeignKey(e => e.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
