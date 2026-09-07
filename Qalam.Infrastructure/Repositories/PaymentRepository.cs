using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class PaymentRepository : GenericRepositoryAsync<Payment>, IPaymentRepository
{
    private readonly ApplicationDBContext _context;

    public PaymentRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdWithItemsAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.PaymentItems)
            .Include(p => p.EnrollmentPayments)
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
    }

    public async Task<Payment?> GetByProviderTransactionIdAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerTransactionId))
            return null;

        return await _context.Payments
            .Include(p => p.PaymentItems)
            .Include(p => p.EnrollmentPayments)
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(
                p => p.ProviderTransactionId == providerTransactionId,
                cancellationToken);
    }
}
