using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class OpenSessionOfferConfiguration : IEntityTypeConfiguration<OpenSessionOffer>
{
    public void Configure(EntityTypeBuilder<OpenSessionOffer> builder)
    {
        builder.ToTable("SessionOffers", "sr");

        builder.HasKey(e => e.Id);

        // One ACTIVE offer per teacher per request. Terminal statuses are history and
        // allow re-offer, so the unique index only covers Pending + Accepted.
        builder.HasIndex(e => new { e.SessionRequestId, e.TeacherId })
               .IsUnique()
               .HasFilter(
                   $"[Status] IN ({(int)OpenSessionOfferStatus.Pending},{(int)OpenSessionOfferStatus.Accepted})");

        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.Status);
        // Used by the background expiry job.
        builder.HasIndex(e => new { e.Status, e.ExpiresAt });

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.Version).IsRequired();

        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Conversation)
               .WithOne(c => c.OpenSessionOffer)
               .HasForeignKey<OfferConversation>(c => c.SessionOfferId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
