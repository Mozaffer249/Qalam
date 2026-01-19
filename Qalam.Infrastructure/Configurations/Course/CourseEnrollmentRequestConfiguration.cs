using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseEnrollmentRequestConfiguration : IEntityTypeConfiguration<CourseEnrollmentRequest>
{
    public void Configure(EntityTypeBuilder<CourseEnrollmentRequest> builder)
    {
        builder.ToTable("CourseEnrollmentRequests", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => e.RequestedByStudentId);
        builder.HasIndex(e => e.Status);
        
        // Properties
        builder.Property(e => e.Notes).HasMaxLength(400);
        
        // Relationships
        builder.HasOne(e => e.Course)
               .WithMany(c => c.CourseEnrollmentRequests)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.RequestedByStudent)
               .WithMany()
               .HasForeignKey(e => e.RequestedByStudentId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasMany(e => e.SelectedAvailabilities)
               .WithOne(sa => sa.CourseEnrollmentRequest)
               .HasForeignKey(sa => sa.CourseEnrollmentRequestId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(e => e.GroupMembers)
               .WithOne(gm => gm.CourseEnrollmentRequest)
               .HasForeignKey(gm => gm.CourseEnrollmentRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
