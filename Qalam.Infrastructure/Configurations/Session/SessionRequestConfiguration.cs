using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Session;

namespace Qalam.Infrastructure.Configurations.Session;

public class SessionRequestConfiguration : IEntityTypeConfiguration<SessionRequest>
{
    public void Configure(EntityTypeBuilder<SessionRequest> builder)
    {
        builder.ToTable("SessionRequests", "session");

        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.SubjectId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.TeachingModeId, e.SessionTypeId });

        // Properties
        builder.Property(e => e.Description).HasMaxLength(800);

        // Relationships
        builder.HasOne(e => e.Student)
               .WithMany()
               .HasForeignKey(e => e.StudentId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path

        builder.HasOne(e => e.Subject)
               .WithMany()
               .HasForeignKey(e => e.SubjectId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Curriculum)
               .WithMany()
               .HasForeignKey(e => e.CurriculumId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Level)
               .WithMany()
               .HasForeignKey(e => e.LevelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeachingMode)
               .WithMany()
               .HasForeignKey(e => e.TeachingModeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SessionType)
               .WithMany()
               .HasForeignKey(e => e.SessionTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
               .WithMany()
               .HasForeignKey(e => e.LocationId)
               .OnDelete(DeleteBehavior.Restrict);

        // Collections
        builder.HasMany(e => e.Offers)
               .WithOne(o => o.SessionRequest)
               .HasForeignKey(o => o.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Sessions)
               .WithOne(s => s.SessionRequest)
               .HasForeignKey(s => s.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
