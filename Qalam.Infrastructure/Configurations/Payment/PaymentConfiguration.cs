using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qalam.Infrastructure.Configurations.Payment;

public class PaymentConfiguration : IEntityTypeConfiguration<Qalam.Data.Entity.Payment.Payment>
{
    public void Configure(EntityTypeBuilder<Qalam.Data.Entity.Payment.Payment> builder)
    {
        builder.Property(e => e.ProviderInvoiceId).HasMaxLength(120);

        builder.HasIndex(e => new { e.PaymentProvider, e.ProviderTransactionId })
            .HasFilter("[ProviderTransactionId] IS NOT NULL");

        builder.HasIndex(e => new { e.PaymentProvider, e.ProviderInvoiceId })
            .HasFilter("[ProviderInvoiceId] IS NOT NULL");
    }
}
