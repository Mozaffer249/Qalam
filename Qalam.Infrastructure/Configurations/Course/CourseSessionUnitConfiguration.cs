using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseSessionUnitConfiguration : IEntityTypeConfiguration<CourseSessionUnit>
{
    public void Configure(EntityTypeBuilder<CourseSessionUnit> builder)
    {
        builder.ToTable("CourseSessionUnits", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseSessionId);
        builder.HasIndex(e => e.ContentUnitId);
        builder.HasIndex(e => e.LessonId);

        builder.HasOne(e => e.CourseSession)
               .WithMany(s => s.Units)
               .HasForeignKey(e => e.CourseSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ContentUnit)
               .WithMany()
               .HasForeignKey(e => e.ContentUnitId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
               .WithMany()
               .HasForeignKey(e => e.LessonId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
