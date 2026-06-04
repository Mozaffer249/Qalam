using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class OpenSessionRequestConfiguration : IEntityTypeConfiguration<OpenSessionRequest>
{
    public void Configure(EntityTypeBuilder<OpenSessionRequest> builder)
    {
        builder.ToTable("SessionRequests", "sr");

        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.RequestedByUserId);
        builder.HasIndex(e => e.CreatedByGuardianId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.ExpiresAt });
        builder.HasIndex(e => e.SubjectId);
        builder.HasIndex(e => e.DomainId);
        builder.HasIndex(e => e.TargetedTeacherId);

        // Properties
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.TotalSessionsCount).IsRequired();

        // Relationships
        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RequestedByUser)
               .WithMany()
               .HasForeignKey(e => e.RequestedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByGuardian)
               .WithMany()
               .HasForeignKey(e => e.CreatedByGuardianId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Domain)
               .WithMany()
               .HasForeignKey(e => e.DomainId)
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

        builder.HasOne(e => e.Term)
               .WithMany()
               .HasForeignKey(e => e.TermId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Subject)
               .WithMany()
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeachingMode)
               .WithMany()
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TargetedTeacher)
               .WithMany()
               .HasForeignKey(e => e.TargetedTeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Sessions)
               .WithOne(s => s.OpenSessionRequest)
               .HasForeignKey(s => s.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
               .WithOne(a => a.OpenSessionRequest)
               .HasForeignKey(a => a.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Invitations)
               .WithOne(i => i.OpenSessionRequest)
               .HasForeignKey(i => i.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Targets)
               .WithOne(t => t.OpenSessionRequest)
               .HasForeignKey(t => t.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // Offers: Restrict — preserve offer history even if request is soft-deleted later.
        builder.HasMany(e => e.Offers)
               .WithOne(o => o.OpenSessionRequest)
               .HasForeignKey(o => o.SessionRequestId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
