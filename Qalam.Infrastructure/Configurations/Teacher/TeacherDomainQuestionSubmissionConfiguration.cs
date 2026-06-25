using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherDomainQuestionSubmissionConfiguration : IEntityTypeConfiguration<TeacherDomainQuestionSubmission>
{
    public void Configure(EntityTypeBuilder<TeacherDomainQuestionSubmission> builder)
    {
        builder.ToTable("TeacherDomainQuestionSubmissions", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TeacherId, e.QuestionId }).IsUnique();
        builder.HasIndex(e => e.VerificationStatus);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Question)
            .WithMany(q => q.Submissions)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeacherDocument)
            .WithMany()
            .HasForeignKey(e => e.TeacherDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
