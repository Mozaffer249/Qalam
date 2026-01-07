using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> builder)
    {
        builder.ToTable("TeacherSubjects", "education");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => new { e.TeacherId, e.SubjectId }).IsUnique();
        
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

