using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Session;

namespace Qalam.Infrastructure.Configurations.Session;

public class SessionRequestOfferConfiguration : IEntityTypeConfiguration<SessionRequestOffer>
{
    public void Configure(EntityTypeBuilder<SessionRequestOffer> builder)
    {
        builder.ToTable("SessionRequestOffers", "session");
        
        builder.HasKey(e => e.Id);
        
        // Indexes
        builder.HasIndex(e => e.SessionRequestId);
        builder.HasIndex(e => e.TeacherId);
        builder.HasIndex(e => e.Status);
        
        // Properties
        builder.Property(e => e.ProposedPrice).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedSchedule).HasMaxLength(800);
        builder.Property(e => e.Notes).HasMaxLength(500);
        
        // Relationships
        builder.HasOne(e => e.SessionRequest)
               .WithMany(sr => sr.Offers)
               .HasForeignKey(e => e.SessionRequestId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Teacher)
               .WithMany()
               .HasForeignKey(e => e.TeacherId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascade path
    }
}
