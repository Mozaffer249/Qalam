using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(50);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.LevelId);
        builder.HasIndex(e => new { e.LevelId, e.NameEn }).IsUnique();

        builder.HasOne(e => e.Level)
               .WithMany(l => l.Grades)
               .HasForeignKey(e => e.LevelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Subjects)
               .WithOne(s => s.Grade)
               .HasForeignKey(s => s.GradeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

