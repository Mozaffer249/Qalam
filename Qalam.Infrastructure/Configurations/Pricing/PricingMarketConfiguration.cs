using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Pricing;

namespace Qalam.Infrastructure.Configurations.Pricing;

public class PricingMarketConfiguration : IEntityTypeConfiguration<PricingMarket>
{
    public void Configure(EntityTypeBuilder<PricingMarket> builder)
    {
        builder.ToTable("PricingMarkets", "pricing");
        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasMaxLength(10);
        builder.Property(e => e.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(e => e.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.ExchangeRateFromBase).HasPrecision(18, 6);
        builder.HasIndex(e => e.IsDefault);
    }
}
