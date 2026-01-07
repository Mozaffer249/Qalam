using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Quran;

namespace Qalam.Infrastructure.Configurations.Quran;

public class QuranContentTypeConfiguration : IEntityTypeConfiguration<QuranContentType>
{
    public void Configure(EntityTypeBuilder<QuranContentType> builder)
    {
        builder.ToTable("QuranContentTypes", "quran");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(50);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Code).IsRequired().HasMaxLength(30);
    }
}

