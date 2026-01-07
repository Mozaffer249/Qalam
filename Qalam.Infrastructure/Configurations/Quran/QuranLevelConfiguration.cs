using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Quran;

namespace Qalam.Infrastructure.Configurations.Quran;

public class QuranLevelConfiguration : IEntityTypeConfiguration<QuranLevel>
{
    public void Configure(EntityTypeBuilder<QuranLevel> builder)
    {
        builder.ToTable("QuranLevels", "quran");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.OrderIndex);
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(50);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DescriptionAr).HasMaxLength(300);
        builder.Property(e => e.DescriptionEn).HasMaxLength(300);
    }
}

