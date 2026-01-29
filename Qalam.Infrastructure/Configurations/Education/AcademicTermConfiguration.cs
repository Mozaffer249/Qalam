using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.ToTable("AcademicTerms", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(50);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(50);
        builder.Property(e => e.IsMandatory).HasDefaultValue(true);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.CurriculumId);
        builder.HasIndex(e => new { e.CurriculumId, e.NameEn }).IsUnique();

        builder.HasOne(e => e.Curriculum)
               .WithMany(c => c.AcademicTerms)
               .HasForeignKey(e => e.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Subjects)
               .WithOne(s => s.Term)
               .HasForeignKey(s => s.TermId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

