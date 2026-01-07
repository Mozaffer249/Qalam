using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Infrastructure.Configurations.Education;

public class EducationDomainConfiguration : IEntityTypeConfiguration<EducationDomain>
{
    public void Configure(EntityTypeBuilder<EducationDomain> builder)
    {
        builder.ToTable("EducationDomains", "education");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DescriptionAr).HasMaxLength(500);
        builder.Property(e => e.DescriptionEn).HasMaxLength(500);
        
        builder.HasMany(e => e.EducationLevels)
               .WithOne(l => l.Domain)
               .HasForeignKey(l => l.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.Subjects)
               .WithOne(s => s.Domain)
               .HasForeignKey(s => s.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.DomainTeachingModes)
               .WithOne(dtm => dtm.Domain)
               .HasForeignKey(dtm => dtm.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.EducationRule)
               .WithOne(r => r.Domain)
               .HasForeignKey<EducationRule>(r => r.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

