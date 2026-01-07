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
        
        builder.HasIndex(e => new { e.SubjectId, e.OrderIndex });
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(200);
        
        builder.HasOne(e => e.Subject)
               .WithMany(s => s.ContentUnits)
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.Lessons)
               .WithOne(l => l.Unit)
               .HasForeignKey(l => l.UnitId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

