using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherDomainQuestionSubmissionDocumentConfiguration
    : IEntityTypeConfiguration<TeacherDomainQuestionSubmissionDocument>
{
    public void Configure(EntityTypeBuilder<TeacherDomainQuestionSubmissionDocument> builder)
    {
        builder.ToTable("TeacherDomainQuestionSubmissionDocuments", "teacher");

        builder.HasKey(e => new { e.SubmissionId, e.TeacherDocumentId });

        builder.HasIndex(e => e.TeacherDocumentId).IsUnique();

        builder.HasOne(e => e.Submission)
            .WithMany(s => s.Documents)
            .HasForeignKey(e => e.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TeacherDocument)
            .WithMany()
            .HasForeignKey(e => e.TeacherDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
