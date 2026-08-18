using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectGradeConfiguration : IEntityTypeConfiguration<TeacherSubjectGrade>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectGrade> builder)
    {
        builder.ToTable("TeacherSubjectGrades", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TeacherSubjectId, e.GradeId }).IsUnique();

        builder.HasOne(e => e.TeacherSubject)
            .WithMany(ts => ts.Grades)
            .HasForeignKey(e => e.TeacherSubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Grade)
            .WithMany()
            .HasForeignKey(e => e.GradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
