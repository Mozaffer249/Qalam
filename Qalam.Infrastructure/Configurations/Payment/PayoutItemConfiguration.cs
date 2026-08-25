using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Configurations.Payment;

public class PayoutItemConfiguration : IEntityTypeConfiguration<PayoutItem>
{
    public void Configure(EntityTypeBuilder<PayoutItem> builder)
    {
        builder.ToTable("PayoutItems");

        builder.HasKey(i => i.Id);

        builder.HasIndex(i => i.PayoutBatchId);
        builder.HasIndex(i => i.TeacherId);

        builder.Property(i => i.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();

        builder.HasOne(i => i.Teacher)
            .WithMany()
            .HasForeignKey(i => i.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
