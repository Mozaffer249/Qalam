using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherLevelConfiguration : IEntityTypeConfiguration<TeacherLevel>
{
    public void Configure(EntityTypeBuilder<TeacherLevel> builder)
    {
        builder.ToTable("TeacherLevels", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.OrderIndex);

        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.NameAr).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TeacherSharePct).HasColumnType("decimal(5,2)").IsRequired();
    }
}
