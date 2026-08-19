using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Pricing;

namespace Qalam.Infrastructure.Configurations.Pricing;

public class PricingSnapshotConfiguration : IEntityTypeConfiguration<PricingSnapshot>
{
    public void Configure(EntityTypeBuilder<PricingSnapshot> builder)
    {
        builder.ToTable("PricingSnapshots", "pricing");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.Context, e.ContextEntityId }).IsUnique();

        builder.Property(e => e.SessionTypeCode).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PricePerHour).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.TeacherSharePct).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(e => e.TeacherEarnings).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.PlatformShare).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(e => e.DomainSessionPrice)
            .WithMany()
            .HasForeignKey(e => e.DomainSessionPriceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
