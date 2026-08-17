using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectEducationLevelConfiguration : IEntityTypeConfiguration<TeacherSubjectEducationLevel>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectEducationLevel> builder)
    {
        builder.ToTable("TeacherSubjectEducationLevels", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TeacherSubjectId, e.EducationLevelId }).IsUnique();

        builder.HasOne(e => e.TeacherSubject)
            .WithMany(ts => ts.EducationLevels)
            .HasForeignKey(e => e.TeacherSubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EducationLevel)
            .WithMany()
            .HasForeignKey(e => e.EducationLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
