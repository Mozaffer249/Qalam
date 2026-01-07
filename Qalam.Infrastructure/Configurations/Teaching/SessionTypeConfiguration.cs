using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Infrastructure.Configurations.Teaching;

public class SessionTypeConfiguration : IEntityTypeConfiguration<SessionType>
{
    public void Configure(EntityTypeBuilder<SessionType> builder)
    {
        builder.ToTable("SessionTypes", "teaching");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.Code).IsUnique();
        
        builder.Property(e => e.Code).IsRequired().HasMaxLength(30);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(50);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DescriptionAr).HasMaxLength(200);
        builder.Property(e => e.DescriptionEn).HasMaxLength(200);
    }
}

