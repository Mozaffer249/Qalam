using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class EducationLevelConfiguration : IEntityTypeConfiguration<EducationLevel>
{
    public void Configure(EntityTypeBuilder<EducationLevel> builder)
    {
        builder.ToTable("EducationLevels", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.DomainId);
        builder.HasIndex(e => e.CurriculumId);
        builder.HasIndex(e => new { e.DomainId, e.CurriculumId, e.NameEn }).IsUnique();

        builder.HasOne(e => e.Domain)
               .WithMany(d => d.EducationLevels)
               .HasForeignKey(e => e.DomainId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Curriculum)
               .WithMany(c => c.EducationLevels)
               .HasForeignKey(e => e.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Grades)
               .WithOne(g => g.Level)
               .HasForeignKey(g => g.LevelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Subjects)
               .WithOne(s => s.Level)
               .HasForeignKey(s => s.LevelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

