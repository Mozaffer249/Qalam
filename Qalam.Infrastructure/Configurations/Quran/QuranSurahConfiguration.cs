using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Quran;

namespace Qalam.Infrastructure.Configurations.Quran;

public class QuranSurahConfiguration : IEntityTypeConfiguration<QuranSurah>
{
    public void Configure(EntityTypeBuilder<QuranSurah> builder)
    {
        builder.ToTable("QuranSurahs", "quran");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.SurahNumber).IsUnique();
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
    }
}

