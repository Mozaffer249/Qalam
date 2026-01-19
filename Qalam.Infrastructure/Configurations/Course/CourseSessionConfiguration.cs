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
        
        // Indexes
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => new { e.CourseId, e.SessionNumber })
               .IsUnique();
        
        // Properties
        builder.Property(e => e.Title).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Description).HasMaxLength(500);
        
        // Relationships
        builder.HasOne(e => e.Course)
               .WithMany(c => c.CourseSessions)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
