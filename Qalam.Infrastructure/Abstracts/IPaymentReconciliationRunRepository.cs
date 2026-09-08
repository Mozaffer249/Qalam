using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface IPaymentReconciliationRunRepository
{
    Task<PaymentReconciliationRun> AddAsync(
        PaymentReconciliationRun run,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(PaymentReconciliationRun run, CancellationToken cancellationToken = default);

    Task<PaymentReconciliationRun?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ScheduleKeyExistsAsync(string scheduleKey, CancellationToken cancellationToken = default);

    IQueryable<PaymentReconciliationRun> GetQuery();
}
