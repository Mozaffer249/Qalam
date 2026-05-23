using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.OpenSessionRequests;

namespace Qalam.Infrastructure.Configurations.OpenSessionRequests;

public class OfferMessageConfiguration : IEntityTypeConfiguration<OfferMessage>
{
    public void Configure(EntityTypeBuilder<OfferMessage> builder)
    {
        builder.ToTable("OfferMessages", "sr");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.OfferConversationId, e.SentAt });
        builder.HasIndex(e => e.SenderUserId);

        builder.Property(e => e.Content).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.MessageType).IsRequired();
        builder.Property(e => e.SentAt).IsRequired();

        builder.HasOne(e => e.SenderUser)
               .WithMany()
               .HasForeignKey(e => e.SenderUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
