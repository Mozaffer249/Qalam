using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common.Enums;
using TeacherEntity = Qalam.Data.Entity.Teacher.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherConfiguration : IEntityTypeConfiguration<TeacherEntity>
{
    public void Configure(EntityTypeBuilder<TeacherEntity> builder)
    {
        builder.Property(e => e.CustomTeacherSharePct).HasColumnType("decimal(5,2)");
        builder.Property(e => e.InterviewUnlockSource).HasDefaultValue(InterviewUnlockSource.None);
        builder.HasIndex(e => e.InterviewUnlockCourseScheduleId);

        builder.HasOne(e => e.TeacherLevel)
            .WithMany()
            .HasForeignKey(e => e.TeacherLevelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
