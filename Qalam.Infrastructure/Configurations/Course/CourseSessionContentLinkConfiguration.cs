using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseSessionContentLinkConfiguration : IEntityTypeConfiguration<CourseSessionContentLink>
{
    public void Configure(EntityTypeBuilder<CourseSessionContentLink> builder)
    {
        builder.ToTable("CourseSessionContentLinks", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.CourseSessionId, e.ContentItemId }).IsUnique();

        builder.HasOne(e => e.CourseSession)
            .WithMany(e => e.ContentLinks)
            .HasForeignKey(e => e.CourseSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ContentItem)
            .WithMany(e => e.CourseSessionLinks)
            .HasForeignKey(e => e.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
