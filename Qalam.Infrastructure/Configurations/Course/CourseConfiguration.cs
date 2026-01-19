using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseConfiguration : IEntityTypeConfiguration<Qalam.Data.Entity.Course.Course>
{
    public void Configure(EntityTypeBuilder<Qalam.Data.Entity.Course.Course> builder)
    {
        builder.ToTable("Courses", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.SubjectId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.TeachingModeId, e.SessionTypeId });
        
        // Properties
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
        
        // Relationships
        builder.HasOne(e => e.Teacher)
               .WithMany(t => t.Courses)
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasOne(e => e.Subject)
               .WithMany()
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Curriculum)
               .WithMany()
               .HasForeignKey(e => e.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Level)
               .WithMany()
               .HasForeignKey(e => e.LevelId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Grade)
               .WithMany()
               .HasForeignKey(e => e.GradeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.TeachingMode)
               .WithMany()
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.SessionType)
               .WithMany()
               .HasForeignKey(e => e.SessionTypeId)
               .OnDelete(DeleteBehavior.Restrict);
        
        // Collections
        builder.HasMany(e => e.CourseSessions)
               .WithOne(cs => cs.Course)
               .HasForeignKey(cs => cs.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.CourseEnrollmentRequests)
               .WithOne(cer => cer.Course)
               .HasForeignKey(cer => cer.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.CourseEnrollments)
               .WithOne(ce => ce.Course)
               .HasForeignKey(ce => ce.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
