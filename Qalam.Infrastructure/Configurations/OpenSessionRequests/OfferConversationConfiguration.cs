using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class OfferConversationConfiguration : IEntityTypeConfiguration<OfferConversation>
{
    public void Configure(EntityTypeBuilder<OfferConversation> builder)
    {
        builder.ToTable("OfferConversations", "sr");

        builder.HasKey(e => e.Id);

        // One conversation per (request, teacher) pair — supports preliminary chat before any offer
        // is submitted. SessionOfferId becomes a denormalized pointer set when an offer lands.
        builder.HasIndex(e => new { e.SessionRequestId, e.TeacherId }).IsUnique();
        builder.HasIndex(e => e.SessionOfferId);
        builder.HasIndex(e => e.LastMessageAt);

        builder.HasOne(e => e.OpenSessionRequest)
               .WithMany()
               .HasForeignKey(e => e.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        // Reverse 1:1 to OpenSessionOffer is configured in SessionOfferConfiguration (nullable now).

        builder.HasMany(e => e.Messages)
               .WithOne(m => m.OfferConversation)
               .HasForeignKey(m => m.OfferConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
