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

        // One conversation per offer.
        builder.HasIndex(e => e.SessionOfferId).IsUnique();
        builder.HasIndex(e => e.LastMessageAt);

        // Reverse relationship to OpenSessionOffer is configured in SessionOfferConfiguration.

        builder.HasMany(e => e.Messages)
               .WithOne(m => m.OfferConversation)
               .HasForeignKey(m => m.OfferConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
