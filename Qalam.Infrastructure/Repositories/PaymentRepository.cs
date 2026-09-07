using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
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

    public async Task<Payment?> GetOpenIntentForEnrollmentAsync(
        int enrollmentId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        if (enrollmentId <= 0 || string.IsNullOrWhiteSpace(provider))
            return null;

        return await _context.Payments
            .Include(p => p.PaymentItems)
            .Where(p => p.Status == PaymentStatus.Pending
                        && p.PaymentProvider == provider
                        && p.PaymentItems.Any(i =>
                            i.ItemType == PaymentItemType.CourseEnrollment
                            && i.ReferenceId == enrollmentId))
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CancelOpenIntentsForEnrollmentAsync(
        int enrollmentId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        if (enrollmentId <= 0 || string.IsNullOrWhiteSpace(provider))
            return;

        var open = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Pending
                        && p.PaymentProvider == provider
                        && p.PaymentItems.Any(i =>
                            i.ItemType == PaymentItemType.CourseEnrollment
                            && i.ReferenceId == enrollmentId))
            .ToListAsync(cancellationToken);

        if (open.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var payment in open)
        {
            payment.Status = PaymentStatus.Cancelled;
            payment.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<Payment> GetPayerHistoryQuery(int payerUserId)
    {
        return _context.Payments
            .AsNoTracking()
            .Include(p => p.PaymentItems)
            .Include(p => p.Refunds)
            .Include(p => p.EnrollmentPayments)
                .ThenInclude(ep => ep.EnrollmentParticipant)
                    .ThenInclude(ep => ep.Enrollment)
            .Where(p => p.PayerUserId == payerUserId)
            .OrderByDescending(p => p.Id);
    }

    public async Task<Payment?> GetReceiptAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.PaymentItems)
            .Include(p => p.Refunds)
            .Include(p => p.EnrollmentPayments)
                .ThenInclude(ep => ep.EnrollmentParticipant)
                    .ThenInclude(ep => ep.Enrollment!)
                        .ThenInclude(e => e.Course!)
                            .ThenInclude(c => c.Teacher!)
                                .ThenInclude(t => t.User)
            .Include(p => p.EnrollmentPayments)
                .ThenInclude(ep => ep.EnrollmentParticipant)
                    .ThenInclude(ep => ep.Enrollment!)
                        .ThenInclude(e => e.ApprovedByTeacher!)
                            .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
    }
}
