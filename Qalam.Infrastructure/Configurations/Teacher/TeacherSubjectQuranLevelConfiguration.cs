using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectQuranLevelConfiguration : IEntityTypeConfiguration<TeacherSubjectQuranLevel>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectQuranLevel> builder)
    {
        builder.ToTable("TeacherSubjectQuranLevels", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TeacherSubjectId, e.QuranLevelId }).IsUnique();

        builder.HasOne(e => e.TeacherSubject)
               .WithMany(ts => ts.QuranLevels)
               .HasForeignKey(e => e.TeacherSubjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.QuranLevel)
               .WithMany()
               .HasForeignKey(e => e.QuranLevelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
