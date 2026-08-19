using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Pricing;

namespace Qalam.Infrastructure.Configurations.Pricing;

public class DomainSessionPriceConfiguration : IEntityTypeConfiguration<DomainSessionPrice>
{
    public void Configure(EntityTypeBuilder<DomainSessionPrice> builder)
    {
        builder.ToTable("DomainSessionPrices", "pricing");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.DomainId, e.SessionTypeCode, e.EffectiveFrom });
        builder.HasIndex(e => new { e.DomainId, e.SessionTypeCode, e.EffectiveTo });

        builder.Property(e => e.SessionTypeCode).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PricePerHour).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(e => e.Domain)
            .WithMany()
            .HasForeignKey(e => e.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
