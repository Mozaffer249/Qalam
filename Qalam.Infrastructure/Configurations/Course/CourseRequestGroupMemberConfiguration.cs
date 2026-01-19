using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Course;

namespace Qalam.Infrastructure.Configurations.Course;

public class CourseRequestGroupMemberConfiguration : IEntityTypeConfiguration<CourseRequestGroupMember>
{
    public void Configure(EntityTypeBuilder<CourseRequestGroupMember> builder)
    {
        builder.ToTable("CourseRequestGroupMembers", "course");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.CourseEnrollmentRequestId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.InvitedByStudentId);
        builder.HasIndex(e => new { e.CourseEnrollmentRequestId, e.StudentId })
               .IsUnique();
        
        // Relationships
        builder.HasOne(e => e.CourseEnrollmentRequest)
               .WithMany(cer => cer.GroupMembers)
               .HasForeignKey(e => e.CourseEnrollmentRequestId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
        
        builder.HasOne(e => e.InvitedByStudent)
               .WithMany()
               .HasForeignKey(e => e.InvitedByStudentId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
    }
}
