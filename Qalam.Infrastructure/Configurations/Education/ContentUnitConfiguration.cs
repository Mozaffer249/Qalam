using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class ContentUnitConfiguration : IEntityTypeConfiguration<ContentUnit>
{
    public void Configure(EntityTypeBuilder<ContentUnit> builder)
    {
        builder.ToTable("ContentUnits", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.UnitTypeCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => new { e.SubjectId, e.UnitTypeCode, e.OrderIndex });
        builder.HasIndex(e => e.QuranSurahId);
        builder.HasIndex(e => e.QuranPartId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => new { e.SubjectId, e.UnitTypeCode, e.NameEn }).IsUnique();

        builder.HasOne(e => e.Subject)
               .WithMany(s => s.ContentUnits)
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.QuranSurah)
               .WithMany()
               .HasForeignKey(e => e.QuranSurahId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.QuranPart)
               .WithMany()
               .HasForeignKey(e => e.QuranPartId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Lessons)
               .WithOne(l => l.Unit)
               .HasForeignKey(l => l.UnitId)
               .OnDelete(DeleteBehavior.Cascade);

        // Check constraints for Quran link validation
        builder.ToTable(t => t.HasCheckConstraint("CK_ContentUnits_QuranSurahLink",
            "([UnitTypeCode] <> 'QuranSurah') OR ([QuranSurahId] IS NOT NULL)"));
        builder.ToTable(t => t.HasCheckConstraint("CK_ContentUnits_QuranPartLink",
            "([UnitTypeCode] <> 'QuranPart') OR ([QuranPartId] IS NOT NULL)"));
    }
}

