using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseSessionConfiguration : IEntityTypeConfiguration<CourseSession>
{
    public void Configure(EntityTypeBuilder<CourseSession> builder)
    {
        builder.ToTable("CourseSessions", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => new { e.CourseId, e.SessionNumber }).IsUnique();

        builder.Property(e => e.Title).HasMaxLength(150);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Course)
               .WithMany(c => c.Sessions)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.QuranContentType)
               .WithMany()
               .HasForeignKey(e => e.QuranContentTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.QuranLevel)
               .WithMany()
               .HasForeignKey(e => e.QuranLevelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
