using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class SessionRequestTargetConfiguration : IEntityTypeConfiguration<OpenSessionRequestTarget>
{
    public void Configure(EntityTypeBuilder<OpenSessionRequestTarget> builder)
    {
        builder.ToTable("SessionRequestTargets", "sr");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.SessionRequestId, e.TeacherId }).IsUnique();
        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => new { e.TeacherId, e.Status });

        builder.Property(e => e.Status).IsRequired();

        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
