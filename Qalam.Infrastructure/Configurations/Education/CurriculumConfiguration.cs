using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.ToTable("Curriculums", "education");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.Country);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Country).HasMaxLength(100);
        builder.Property(e => e.DescriptionAr).HasMaxLength(500);
        builder.Property(e => e.DescriptionEn).HasMaxLength(500);
        
        builder.HasMany(e => e.EducationLevels)
               .WithOne(l => l.Curriculum)
               .HasForeignKey(l => l.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.AcademicTerms)
               .WithOne(t => t.Curriculum)
               .HasForeignKey(t => t.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.Subjects)
               .WithOne(s => s.Curriculum)
               .HasForeignKey(s => s.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

