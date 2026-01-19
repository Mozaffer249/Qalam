using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.ToTable("CourseEnrollments", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.ApprovedByTeacherId);
        builder.HasIndex(e => e.EnrollmentStatus);
        builder.HasIndex(e => new { e.CourseId, e.StudentId })
               .IsUnique();
        
        // Relationships
        builder.HasOne(e => e.Course)
               .WithMany(c => c.CourseEnrollments)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasOne(e => e.ApprovedByTeacher)
               .WithMany()
               .HasForeignKey(e => e.ApprovedByTeacherId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasMany(e => e.CourseSchedules)
               .WithOne(cs => cs.CourseEnrollment)
               .HasForeignKey(cs => cs.CourseEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.CourseEnrollmentPayments)
               .WithOne(cep => cep.CourseEnrollment)
               .HasForeignKey(cep => cep.CourseEnrollmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
