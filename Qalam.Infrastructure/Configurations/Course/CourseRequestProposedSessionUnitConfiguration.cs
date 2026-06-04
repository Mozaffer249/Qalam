using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseRequestProposedSessionUnitConfiguration
    : IEntityTypeConfiguration<CourseRequestProposedSessionUnit>
{
    public void Configure(EntityTypeBuilder<CourseRequestProposedSessionUnit> builder)
    {
        builder.ToTable("CourseRequestProposedSessionUnits", "course");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.CourseRequestProposedSessionId);
        builder.HasIndex(e => e.ContentUnitId);
        builder.HasIndex(e => e.LessonId);

        builder.HasOne(e => e.ProposedSession)
               .WithMany(s => s.Units)
               .HasForeignKey(e => e.CourseRequestProposedSessionId)
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
