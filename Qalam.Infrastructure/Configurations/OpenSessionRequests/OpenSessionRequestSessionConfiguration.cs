using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class SessionRequestSessionConfiguration : IEntityTypeConfiguration<OpenSessionRequestSession>
{
    public void Configure(EntityTypeBuilder<OpenSessionRequestSession> builder)
    {
        builder.ToTable("SessionRequestSessions", "sr");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.SessionRequestId, e.SequenceNumber }).IsUnique();
        builder.HasIndex(e => e.PreferredDate);

        builder.Property(e => e.DurationMinutes).IsRequired();

        builder.HasOne(e => e.TimeSlot)
               .WithMany()
               .HasForeignKey(e => e.TimeSlotId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.QuranContentType)
               .WithMany()
               .HasForeignKey(e => e.QuranContentTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.QuranLevel)
               .WithMany()
               .HasForeignKey(e => e.QuranLevelId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Units)
               .WithOne(u => u.OpenSessionRequestSession)
               .HasForeignKey(u => u.SessionRequestSessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
