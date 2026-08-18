using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectFieldLevelConfiguration : IEntityTypeConfiguration<TeacherSubjectFieldLevel>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectFieldLevel> builder)
    {
        builder.ToTable("TeacherSubjectFieldLevels", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TeacherSubjectId, e.WritableFilterValueId, e.EducationLevelId }).IsUnique();

        builder.HasOne(e => e.TeacherSubject)
            .WithMany(ts => ts.FieldLevels)
            .HasForeignKey(e => e.TeacherSubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.WritableFilterValue)
            .WithMany()
            .HasForeignKey(e => e.WritableFilterValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EducationLevel)
            .WithMany()
            .HasForeignKey(e => e.EducationLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
