using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common;

namespace Qalam.Infrastructure.Configurations.Common;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings", "common");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.IsPublic);
        
        builder.Property(e => e.Key).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Value).IsRequired();
        builder.Property(e => e.DescriptionAr).HasMaxLength(200);
        builder.Property(e => e.DescriptionEn).HasMaxLength(200);
    }
}

