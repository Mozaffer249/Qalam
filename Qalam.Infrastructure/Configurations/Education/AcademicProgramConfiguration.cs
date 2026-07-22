using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("AcademicPrograms", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.DegreeType).HasMaxLength(50);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.DepartmentId);
        builder.HasIndex(e => new { e.DepartmentId, e.Code })
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(e => e.IsActive);

        builder.HasOne(e => e.Department)
               .WithMany(d => d.AcademicPrograms)
               .HasForeignKey(e => e.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.EducationLevels)
               .WithOne(l => l.AcademicProgram)
               .HasForeignKey(l => l.AcademicProgramId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.AcademicTerms)
               .WithOne(t => t.AcademicProgram)
               .HasForeignKey(t => t.AcademicProgramId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Subjects)
               .WithOne(s => s.AcademicProgram)
               .HasForeignKey(s => s.AcademicProgramId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
