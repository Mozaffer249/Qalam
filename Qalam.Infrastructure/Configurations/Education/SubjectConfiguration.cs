using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Education;

namespace Qalam.Infrastructure.Configurations.Education;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects", "education");
        
        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.DomainId);
        builder.HasIndex(e => new { e.CurriculumId, e.LevelId, e.GradeId, e.TermId });
        builder.HasIndex(e => e.IsActive);
        
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DescriptionAr).HasMaxLength(500);
        builder.Property(e => e.DescriptionEn).HasMaxLength(500);
        
        builder.HasOne(e => e.Domain)
               .WithMany(d => d.Subjects)
               .HasForeignKey(e => e.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Curriculum)
               .WithMany(c => c.Subjects)
               .HasForeignKey(e => e.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Level)
               .WithMany(l => l.Subjects)
               .HasForeignKey(e => e.LevelId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Grade)
               .WithMany(g => g.Subjects)
               .HasForeignKey(e => e.GradeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Term)
               .WithMany(t => t.Subjects)
               .HasForeignKey(e => e.TermId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(e => e.ContentUnits)
               .WithOne(cu => cu.Subject)
               .HasForeignKey(cu => cu.SubjectId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.TeacherSubjects)
               .WithOne(ts => ts.Subject)
               .HasForeignKey(ts => ts.SubjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

