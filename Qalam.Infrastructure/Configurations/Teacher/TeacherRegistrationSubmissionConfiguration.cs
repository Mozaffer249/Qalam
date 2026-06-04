using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherRegistrationSubmissionConfiguration : IEntityTypeConfiguration<TeacherRegistrationSubmission>
{
    public void Configure(EntityTypeBuilder<TeacherRegistrationSubmission> builder)
    {
        builder.ToTable("TeacherRegistrationSubmissions", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TeacherId, e.RequirementId }).IsUnique();
        builder.HasIndex(e => e.VerificationStatus);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Requirement)
            .WithMany(r => r.Submissions)
            .HasForeignKey(e => e.RequirementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeacherDocument)
            .WithMany()
            .HasForeignKey(e => e.TeacherDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
