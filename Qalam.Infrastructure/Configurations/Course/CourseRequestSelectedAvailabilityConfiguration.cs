using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseRequestSelectedAvailabilityConfiguration : IEntityTypeConfiguration<CourseRequestSelectedAvailability>
{
    public void Configure(EntityTypeBuilder<CourseRequestSelectedAvailability> builder)
    {
        builder.ToTable("CourseRequestSelectedAvailabilities", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.CourseEnrollmentRequestId);
        builder.HasIndex(e => e.TeacherAvailabilityId);
        
        // Relationships
        builder.HasOne(e => e.CourseEnrollmentRequest)
               .WithMany(cer => cer.SelectedAvailabilities)
               .HasForeignKey(e => e.CourseEnrollmentRequestId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.TeacherAvailability)
               .WithMany()
               .HasForeignKey(e => e.TeacherAvailabilityId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
    }
}
