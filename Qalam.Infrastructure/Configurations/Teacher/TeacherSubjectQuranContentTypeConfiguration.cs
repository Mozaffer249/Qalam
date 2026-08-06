using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectQuranContentTypeConfiguration : IEntityTypeConfiguration<TeacherSubjectQuranContentType>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectQuranContentType> builder)
    {
        builder.ToTable("TeacherSubjectQuranContentTypes", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TeacherSubjectId, e.QuranContentTypeId }).IsUnique();

        builder.HasOne(e => e.TeacherSubject)
               .WithMany(ts => ts.QuranContentTypes)
               .HasForeignKey(e => e.TeacherSubjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.QuranContentType)
               .WithMany()
               .HasForeignKey(e => e.QuranContentTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
