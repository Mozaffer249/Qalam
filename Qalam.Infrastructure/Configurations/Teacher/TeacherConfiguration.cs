using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeacherEntity = Qalam.Data.Entity.Teacher.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherConfiguration : IEntityTypeConfiguration<TeacherEntity>
{
    public void Configure(EntityTypeBuilder<TeacherEntity> builder)
    {
        builder.Property(e => e.CustomTeacherSharePct).HasColumnType("decimal(5,2)");

        builder.HasOne(e => e.TeacherLevel)
            .WithMany()
            .HasForeignKey(e => e.TeacherLevelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
