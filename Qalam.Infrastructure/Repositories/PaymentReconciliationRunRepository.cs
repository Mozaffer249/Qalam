using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class PaymentReconciliationRunRepository : IPaymentReconciliationRunRepository
{
    private readonly ApplicationDBContext _context;

    public PaymentReconciliationRunRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<PaymentReconciliationRun> AddAsync(
        PaymentReconciliationRun run,
        CancellationToken cancellationToken = default)
    {
        _context.PaymentReconciliationRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task UpdateAsync(PaymentReconciliationRun run, CancellationToken cancellationToken = default)
    {
        _context.PaymentReconciliationRuns.Update(run);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<PaymentReconciliationRun?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PaymentReconciliationRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ScheduleKeyExistsAsync(string scheduleKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scheduleKey))
            return Task.FromResult(false);

        return _context.PaymentReconciliationRuns
            .AsNoTracking()
            .AnyAsync(r => r.ScheduleKey == scheduleKey, cancellationToken);
    }

    public IQueryable<PaymentReconciliationRun> GetQuery() =>
        _context.PaymentReconciliationRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAt);
}
