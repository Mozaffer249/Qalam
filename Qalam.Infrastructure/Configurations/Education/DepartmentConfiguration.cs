using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "education");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(150);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.CollegeId);
        builder.HasIndex(e => new { e.CollegeId, e.Code })
               .IsUnique()
               .HasFilter("[Code] IS NOT NULL");
        builder.HasIndex(e => e.IsActive);

        builder.HasOne(e => e.College)
               .WithMany(c => c.Departments)
               .HasForeignKey(e => e.CollegeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.AcademicPrograms)
               .WithOne(p => p.Department)
               .HasForeignKey(p => p.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
