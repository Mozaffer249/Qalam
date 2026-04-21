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
    }
}
