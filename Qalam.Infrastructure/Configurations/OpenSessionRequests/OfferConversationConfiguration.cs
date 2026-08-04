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

        builder.Property(e => e.IsOfferScoped)
               .IsRequired()
               .HasDefaultValue(false);

        // Targeted: one conversation per (request, teacher).
        builder.HasIndex(e => new { e.SessionRequestId, e.TeacherId })
               .IsUnique()
               .HasFilter("[IsOfferScoped] = 0");

        // Broadcast (and any offer-linked row): at most one conversation per offer.
        builder.HasIndex(e => e.SessionOfferId)
               .IsUnique()
               .HasFilter("[SessionOfferId] IS NOT NULL");

        builder.HasIndex(e => e.LastMessageAt);
        builder.HasIndex(e => e.TeacherId);

        builder.HasOne(e => e.OpenSessionRequest)
               .WithMany()
               .HasForeignKey(e => e.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Messages)
               .WithOne(m => m.OfferConversation)
               .HasForeignKey(m => m.OfferConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
