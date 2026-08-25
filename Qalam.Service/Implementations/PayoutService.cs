using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PayoutService : IPayoutService
{
    private readonly ApplicationDBContext _db;

    public PayoutService(ApplicationDBContext db)
    {
        _db = db;
    }

    public async Task<List<AdminPendingEarningDto>> ListPendingEarningsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.Status == TeacherEarningLineStatus.Pending)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new AdminPendingEarningDto
            {
                Id = l.Id,
                TeacherId = l.TeacherId,
                TeacherName = l.Teacher.User != null
                    ? ((l.Teacher.User.FirstName ?? "") + " " + (l.Teacher.User.LastName ?? "")).Trim()
                    : null,
                EnrollmentId = l.EnrollmentId,
                CourseScheduleId = l.CourseScheduleId,
                Amount = l.Amount,
                Currency = l.Currency,
                Source = l.Source.ToString(),
                CreatedAt = l.CreatedAt
            })
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminPayoutBatchDto> CreateBatchFromPendingAsync(
        DateTime? periodStart,
        DateTime? periodEnd,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var start = periodStart ?? DateTime.UtcNow.AddMonths(-1);
        var end = periodEnd ?? DateTime.UtcNow;

        var lines = await _db.TeacherEarningLines
            .Where(l => l.Status == TeacherEarningLineStatus.Pending
                        && l.CreatedAt >= start
                        && l.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        if (lines.Count == 0)
            throw new InvalidOperationException("No pending earnings in the selected period.");

        var currency = lines.Select(l => l.Currency).FirstOrDefault() ?? "SAR";
        var byTeacher = lines.GroupBy(l => l.TeacherId).ToList();

        var batch = new PayoutBatch
        {
            PeriodStart = start,
            PeriodEnd = end,
            Currency = currency,
            Status = PayoutBatchStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = 0
        };

        foreach (var group in byTeacher)
        {
            var amount = group.Sum(l => l.Amount);
            var item = new PayoutItem
            {
                TeacherId = group.Key,
                Amount = amount,
                Currency = currency,
                CreatedAt = DateTime.UtcNow
            };
            batch.Items.Add(item);
            batch.TotalAmount += amount;

            foreach (var line in group)
            {
                line.Status = TeacherEarningLineStatus.IncludedInPayout;
                line.PayoutItem = item;
            }
        }

        _db.PayoutBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetBatchAsync(batch.Id, cancellationToken))!;
    }

    public async Task<AdminPayoutBatchDto?> ApproveAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.PayoutBatches
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Draft)
            throw new InvalidOperationException("Only draft batches can be approved.");

        batch.Status = PayoutBatchStatus.Approved;
        batch.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> MarkPaidAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.PayoutBatches
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status is not PayoutBatchStatus.Approved and not PayoutBatchStatus.Draft)
            throw new InvalidOperationException("Batch cannot be marked paid in its current status.");

        batch.Status = PayoutBatchStatus.Paid;
        batch.PaidAt = DateTime.UtcNow;
        batch.ApprovedAt ??= batch.PaidAt;
        batch.MockTransferRef ??= $"MOCK-PAYOUT-{batch.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<List<AdminPayoutBatchListItemDto>> ListBatchesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.PayoutBatches
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new AdminPayoutBatchListItemDto
            {
                Id = b.Id,
                PeriodStart = b.PeriodStart,
                PeriodEnd = b.PeriodEnd,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                Status = b.Status.ToString(),
                MockTransferRef = b.MockTransferRef,
                ItemsCount = b.Items.Count,
                CreatedAt = b.CreatedAt,
                ApprovedAt = b.ApprovedAt,
                PaidAt = b.PaidAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> GetBatchAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.PayoutBatches
            .AsNoTracking()
            .Where(b => b.Id == batchId)
            .Select(b => new AdminPayoutBatchDto
            {
                Id = b.Id,
                PeriodStart = b.PeriodStart,
                PeriodEnd = b.PeriodEnd,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                Status = b.Status.ToString(),
                MockTransferRef = b.MockTransferRef,
                ItemsCount = b.Items.Count,
                CreatedAt = b.CreatedAt,
                ApprovedAt = b.ApprovedAt,
                PaidAt = b.PaidAt,
                Items = b.Items.Select(i => new AdminPayoutItemDto
                {
                    Id = i.Id,
                    TeacherId = i.TeacherId,
                    TeacherName = i.Teacher.User != null
                        ? ((i.Teacher.User.FirstName ?? "") + " " + (i.Teacher.User.LastName ?? "")).Trim()
                        : null,
                    Amount = i.Amount,
                    Currency = i.Currency,
                    LinesCount = i.EarningLines.Count
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return batch;
    }
}
