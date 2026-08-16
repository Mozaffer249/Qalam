using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherSubjectWritableFilterConfiguration : IEntityTypeConfiguration<TeacherSubjectWritableFilter>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectWritableFilter> builder)
    {
        builder.ToTable("TeacherSubjectWritableFilters", "teacher");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TeacherSubjectId, e.WritableFilterValueId }).IsUnique();

        builder.HasOne(e => e.TeacherSubject)
            .WithMany(ts => ts.WritableFilters)
            .HasForeignKey(e => e.TeacherSubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.WritableFilterValue)
            .WithMany()
            .HasForeignKey(e => e.WritableFilterValueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
