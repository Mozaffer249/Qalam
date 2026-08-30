using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PayoutService : IPayoutService
{
    private readonly IPayoutRepository _payouts;

    public PayoutService(IPayoutRepository payouts)
    {
        _payouts = payouts;
    }

    public async Task<PagedResult<AdminPendingEarningDto>> ListPendingEarningsAsync(
        AdminPendingEarningsFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _payouts.ListPendingEarningsAsync(filter, cancellationToken);
        return new PagedResult<AdminPendingEarningDto>
        {
            Items = items,
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 25 : filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AdminPayoutBatchDto> CreateBatchFromPendingAsync(
        DateTime? periodStart,
        DateTime? periodEnd,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var start = periodStart ?? DateTime.UtcNow.AddMonths(-1);
        var end = periodEnd ?? DateTime.UtcNow;

        var lines = await _payouts.GetPendingLinesInPeriodAsync(start, end, cancellationToken);

        if (lines.Count == 0)
            throw new InvalidOperationException("No pending earnings in the selected period.");

        var currency = lines.Select(l => l.Currency).FirstOrDefault() ?? "SAR";
        var byTeacher = lines.GroupBy(l => l.TeacherId).ToList();

        var batch = new PayoutBatch
        {
            PeriodStart = start,
            PeriodEnd = end,
            Currency = currency,
            Status = PayoutBatchStatus.Pending,
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

        await _payouts.AddBatchAsync(batch, cancellationToken);
        await _payouts.SaveChangesAsync(cancellationToken);

        return (await GetBatchAsync(batch.Id, cancellationToken))!;
    }

    public async Task<AdminPayoutBatchDto?> ApproveAsync(
        int batchId,
        int? approvedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Pending)
            throw new InvalidOperationException("Only pending batches can be approved.");

        batch.Status = PayoutBatchStatus.Approved;
        batch.ApprovedAt = DateTime.UtcNow;
        batch.ApprovedByUserId = approvedByUserId;
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> RejectAsync(
        int batchId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedWithLinesAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Pending)
            throw new InvalidOperationException("Only pending batches can be rejected.");

        ReleaseBatchLines(batch);
        batch.Status = PayoutBatchStatus.Rejected;
        batch.RejectedAt = DateTime.UtcNow;
        batch.RejectionReason = reason?.Trim();
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> CancelAsync(
        int batchId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedWithLinesAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status is not PayoutBatchStatus.Pending and not PayoutBatchStatus.Approved)
            throw new InvalidOperationException("Batch cannot be cancelled in its current status.");

        ReleaseBatchLines(batch);
        batch.Status = PayoutBatchStatus.Cancelled;
        batch.CancelledAt = DateTime.UtcNow;
        batch.AdminNotes = reason?.Trim();
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> ProcessAsync(
        int batchId,
        int? processedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Approved)
            throw new InvalidOperationException("Only approved batches can be processed.");

        batch.Status = PayoutBatchStatus.Processing;
        batch.ProcessedAt = DateTime.UtcNow;
        batch.ProcessedByUserId = processedByUserId;
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> MarkPaidAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Processing)
            throw new InvalidOperationException("Only processing batches can be marked paid.");

        batch.Status = PayoutBatchStatus.Paid;
        batch.PaidAt = DateTime.UtcNow;
        batch.MockTransferRef ??= $"MOCK-PAYOUT-{batch.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> MarkFailedAsync(
        int batchId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Processing)
            throw new InvalidOperationException("Only processing batches can be marked failed.");

        batch.Status = PayoutBatchStatus.Failed;
        batch.FailedAt = DateTime.UtcNow;
        batch.FailureReason = reason?.Trim();
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<AdminPayoutBatchDto?> RetryAsync(
        int batchId,
        int? processedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Failed)
            throw new InvalidOperationException("Only failed batches can be retried.");

        batch.Status = PayoutBatchStatus.Processing;
        batch.ProcessedAt = DateTime.UtcNow;
        batch.ProcessedByUserId = processedByUserId;
        batch.FailedAt = null;
        batch.FailureReason = null;
        await _payouts.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batchId, cancellationToken);
    }

    public async Task<PagedResult<AdminPayoutBatchListItemDto>> ListBatchesAsync(
        AdminPayoutListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _payouts.ListBatchesAsync(filter, cancellationToken);
        return new PagedResult<AdminPayoutBatchListItemDto>
        {
            Items = items,
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 25 : filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AdminPayoutBatchDto?> GetBatchAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchWithDetailsAsync(batchId, cancellationToken);
        if (batch == null)
            return null;

        var enrollmentIds = batch.Items
            .SelectMany(i => i.EarningLines)
            .Select(l => l.EnrollmentId)
            .Distinct()
            .ToList();

        var refundsByEnrollment = await _payouts.GetRefundsByEnrollmentIdsAsync(
            enrollmentIds, cancellationToken);
        var commissionByEnrollment = await _payouts.GetCommissionByEnrollmentIdsAsync(
            enrollmentIds, cancellationToken);

        var timeline = BuildTimeline(batch);

        return new AdminPayoutBatchDto
        {
            Id = batch.Id,
            PeriodStart = batch.PeriodStart,
            PeriodEnd = batch.PeriodEnd,
            TotalAmount = batch.TotalAmount,
            Currency = batch.Currency,
            Status = batch.Status.ToString(),
            MockTransferRef = batch.MockTransferRef,
            ItemsCount = batch.Items.Count,
            CreatedAt = batch.CreatedAt,
            UpdatedAt = batch.PaidAt ?? batch.ProcessedAt ?? batch.ApprovedAt ?? batch.CreatedAt,
            ApprovedAt = batch.ApprovedAt,
            PaidAt = batch.PaidAt,
            ProcessedAt = batch.ProcessedAt,
            RejectedAt = batch.RejectedAt,
            CancelledAt = batch.CancelledAt,
            FailedAt = batch.FailedAt,
            RejectionReason = batch.RejectionReason,
            FailureReason = batch.FailureReason,
            AdminNotes = batch.AdminNotes,
            Timeline = timeline,
            Items = batch.Items.Select(i =>
            {
                var lineDtos = i.EarningLines.Select(l =>
                {
                    var schedules = l.Enrollment?.CourseSchedules?
                        .Where(s => s.Status != ScheduleStatus.Cancelled
                                    && s.Status != ScheduleStatus.Rescheduled)
                        .ToList() ?? [];
                    return new AdminPayoutEarningLineDto
                    {
                        LineId = l.Id,
                        EnrollmentId = l.EnrollmentId,
                        CourseTitle = l.Enrollment?.Course?.Title,
                        CourseScheduleId = l.CourseScheduleId,
                        Amount = l.Amount,
                        Source = l.Source.ToString(),
                        Status = l.Status.ToString(),
                        CreatedAt = l.CreatedAt,
                        FreeSessionsInEnrollment = l.Enrollment?.IsFreeTrial == true ? 1 : 0,
                        SessionsCompleted = schedules.Count(s => s.Status == ScheduleStatus.Completed),
                        TransactionKey = $"earn-{l.Id}"
                    };
                }).ToList();

                var itemEnrollmentIds = lineDtos.Select(x => x.EnrollmentId).Distinct().ToList();
                var commission = itemEnrollmentIds.Sum(id =>
                    commissionByEnrollment.GetValueOrDefault(id));
                var refunds = itemEnrollmentIds.Sum(id =>
                    refundsByEnrollment.GetValueOrDefault(id));

                return new AdminPayoutItemDto
                {
                    Id = i.Id,
                    TeacherId = i.TeacherId,
                    TeacherName = i.Teacher?.User != null
                        ? ((i.Teacher.User.FirstName ?? "") + " " + (i.Teacher.User.LastName ?? "")).Trim()
                        : null,
                    Amount = i.Amount,
                    Currency = i.Currency,
                    LinesCount = lineDtos.Count,
                    CommissionAmount = commission,
                    RefundsAmount = refunds,
                    TransferrableAmount = i.Amount,
                    Lines = lineDtos
                };
            }).ToList()
        };
    }

    private static void ReleaseBatchLines(PayoutBatch batch)
    {
        foreach (var line in batch.Items.SelectMany(i => i.EarningLines))
        {
            line.Status = TeacherEarningLineStatus.Pending;
            line.PayoutItemId = null;
        }
    }

    private static List<FinanceTimelineEventDto> BuildTimeline(PayoutBatch batch)
    {
        var timeline = new List<FinanceTimelineEventDto>
        {
            new()
            {
                EventType = "Created",
                Label = "Payout batch created",
                OccurredAt = batch.CreatedAt
            }
        };

        if (batch.ApprovedAt.HasValue)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Approved",
                Label = "Batch approved",
                OccurredAt = batch.ApprovedAt.Value
            });
        }

        if (batch.ProcessedAt.HasValue)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Processing",
                Label = "Transfer processing started",
                OccurredAt = batch.ProcessedAt.Value
            });
        }

        if (batch.PaidAt.HasValue)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Paid",
                Label = "Batch marked paid",
                OccurredAt = batch.PaidAt.Value,
                Notes = batch.MockTransferRef
            });
        }

        if (batch.RejectedAt.HasValue)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Rejected",
                Label = "Batch rejected",
                OccurredAt = batch.RejectedAt.Value,
                Notes = batch.RejectionReason
            });
        }

        if (batch.CancelledAt.HasValue)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Cancelled",
                Label = "Batch cancelled",
                OccurredAt = batch.CancelledAt.Value,
                Notes = batch.AdminNotes
            });
        }

        if (batch.FailedAt.HasValue)
        {
            timeline.Add(new FinanceTimelineEventDto
            {
                EventType = "Failed",
                Label = "Transfer failed",
                OccurredAt = batch.FailedAt.Value,
                Notes = batch.FailureReason
            });
        }

        return timeline;
    }
}
