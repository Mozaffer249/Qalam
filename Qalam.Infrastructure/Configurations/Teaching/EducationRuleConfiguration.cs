using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teaching;

namespace Qalam.Infrastructure.Configurations.Teaching;

public class EducationRuleConfiguration : IEntityTypeConfiguration<EducationRule>
{
    public void Configure(EntityTypeBuilder<EducationRule> builder)
    {
        builder.ToTable("EducationRules", "teaching");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.DomainId).IsUnique();
        
        builder.Property(e => e.NotesAr).HasMaxLength(500);
        builder.Property(e => e.NotesEn).HasMaxLength(500);
        
        builder.HasOne(e => e.Domain)
               .WithOne(d => d.EducationRule)
               .HasForeignKey<EducationRule>(e => e.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

