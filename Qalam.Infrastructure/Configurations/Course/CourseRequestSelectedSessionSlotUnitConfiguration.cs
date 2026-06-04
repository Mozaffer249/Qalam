using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseRequestSelectedSessionSlotUnitConfiguration
    : IEntityTypeConfiguration<CourseRequestSelectedSessionSlotUnit>
{
    public void Configure(EntityTypeBuilder<CourseRequestSelectedSessionSlotUnit> builder)
    {
        builder.ToTable("CourseRequestSelectedSessionSlotUnits", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseRequestSelectedSessionSlotId);
        builder.HasIndex(e => e.ContentUnitId);
        builder.HasIndex(e => e.LessonId);

        builder.HasOne(e => e.SessionSlot)
               .WithMany(s => s.Units)
               .HasForeignKey(e => e.CourseRequestSelectedSessionSlotId)
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
