using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Quran;

namespace Qalam.Infrastructure.Configurations.Quran;

public class QuranPartConfiguration : IEntityTypeConfiguration<QuranPart>
{
    public void Configure(EntityTypeBuilder<QuranPart> builder)
    {
        builder.ToTable("QuranParts", "quran");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.PartNumber).IsUnique();
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
    }
}

