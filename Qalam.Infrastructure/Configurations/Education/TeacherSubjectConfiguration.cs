using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Education;

public class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> builder)
    {
        builder.ToTable("TeacherSubjects", "education");

        builder.HasKey(e => e.Id);

        // Index for performance (not unique - allows multiple TeacherSubjects for same subject with different units)
        builder.HasIndex(e => new { e.TeacherId, e.SubjectId });

        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Subject)
               .WithMany(s => s.TeacherSubjects)
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

