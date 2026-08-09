using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Configurations.Teacher;

public class TeacherDomainApprovalConfiguration : IEntityTypeConfiguration<TeacherDomainApproval>
{
    public void Configure(EntityTypeBuilder<TeacherDomainApproval> builder)
    {
        builder.ToTable("TeacherDomainApprovals", "teacher");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TeacherId, e.DomainId }).IsUnique();
        builder.HasIndex(e => e.RevokedAt);

        builder.Property(e => e.RevokeReason).HasMaxLength(500);

        builder.HasOne(e => e.Teacher)
            .WithMany()
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Domain)
            .WithMany()
            .HasForeignKey(e => e.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
