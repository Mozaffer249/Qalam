using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class SessionHomeworkAssignmentConfiguration : IEntityTypeConfiguration<SessionHomeworkAssignment>
{
    public void Configure(EntityTypeBuilder<SessionHomeworkAssignment> builder)
    {
        builder.ToTable("SessionHomeworkAssignments", "teacher");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasIndex(e => e.CourseScheduleId);

        builder.HasOne(e => e.CourseSchedule)
            .WithMany()
            .HasForeignKey(e => e.CourseScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
