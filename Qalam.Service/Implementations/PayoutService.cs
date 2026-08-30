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

        await _payouts.AddBatchAsync(batch, cancellationToken);
        await _payouts.SaveChangesAsync(cancellationToken);

        return (await GetBatchAsync(batch.Id, cancellationToken))!;
    }

    public async Task<AdminPayoutBatchDto?> ApproveAsync(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _payouts.GetBatchTrackedAsync(batchId, cancellationToken);
        if (batch == null)
            return null;
        if (batch.Status != PayoutBatchStatus.Draft)
            throw new InvalidOperationException("Only draft batches can be approved.");

        batch.Status = PayoutBatchStatus.Approved;
        batch.ApprovedAt = DateTime.UtcNow;
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
        if (batch.Status is not PayoutBatchStatus.Approved and not PayoutBatchStatus.Draft)
            throw new InvalidOperationException("Batch cannot be marked paid in its current status.");

        batch.Status = PayoutBatchStatus.Paid;
        batch.PaidAt = DateTime.UtcNow;
        batch.ApprovedAt ??= batch.PaidAt;
        batch.MockTransferRef ??= $"MOCK-PAYOUT-{batch.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
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
            UpdatedAt = batch.PaidAt ?? batch.ApprovedAt ?? batch.CreatedAt,
            ApprovedAt = batch.ApprovedAt,
            PaidAt = batch.PaidAt,
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
}
