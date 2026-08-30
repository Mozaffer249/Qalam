using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly ApplicationDBContext _context;

    public RefundRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<(List<AdminRefundListItemDto> Items, int TotalCount)> ListAsync(
        AdminRefundListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _context.Refunds.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
            q = q.Where(r => r.Status == filter.Status.Value);
        if (filter.EnrollmentId.HasValue)
            q = q.Where(r => r.EnrollmentId == filter.EnrollmentId.Value);
        if (filter.TeacherId.HasValue)
            q = q.Where(r => r.Enrollment.ApprovedByTeacherId == filter.TeacherId.Value);
        if (filter.StudentId.HasValue)
            q = q.Where(r => r.Enrollment.Participants.Any(p => p.StudentId == filter.StudentId.Value));
        if (filter.FromUtc.HasValue)
            q = q.Where(r => r.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(r => r.CreatedAt <= filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.Id.ToString().Contains(search)
                || r.PaymentId.ToString().Contains(search)
                || r.EnrollmentId.ToString().Contains(search)
                || (r.ProviderRefundId != null && r.ProviderRefundId.ToLower().Contains(search))
                || (r.Enrollment.Course != null && r.Enrollment.Course.Title.ToLower().Contains(search))
                || (r.Payment.PayerUser != null
                    && ((r.Payment.PayerUser.FirstName ?? "") + " " + (r.Payment.PayerUser.LastName ?? ""))
                        .ToLower().Contains(search)));
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            < 1 => 25,
            > 100 => 100,
            _ => filter.PageSize
        };

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRefundListItemDto
            {
                Id = r.Id,
                PaymentId = r.PaymentId,
                EnrollmentId = r.EnrollmentId,
                Amount = r.Amount,
                Currency = r.Currency,
                Reason = r.Reason,
                Status = r.Status.ToString(),
                ProviderRefundId = r.ProviderRefundId,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.Status == RefundStatus.Succeeded ? r.CreatedAt : null,
                CourseTitle = r.Enrollment.Course != null ? r.Enrollment.Course.Title : null,
                PayerName = r.Payment.PayerUser != null
                    ? ((r.Payment.PayerUser.FirstName ?? "") + " " + (r.Payment.PayerUser.LastName ?? "")).Trim()
                    : null,
                TeacherId = r.Enrollment.ApprovedByTeacherId,
                TeacherName = r.Enrollment.ApprovedByTeacher.User != null
                    ? ((r.Enrollment.ApprovedByTeacher.User.FirstName ?? "") + " "
                       + (r.Enrollment.ApprovedByTeacher.User.LastName ?? "")).Trim()
                    : null,
                StudentId = r.Enrollment.Participants.OrderBy(p => p.Id).Select(p => (int?)p.StudentId).FirstOrDefault(),
                StudentName = r.Enrollment.Participants.OrderBy(p => p.Id)
                    .Select(p => p.Student.User != null
                        ? ((p.Student.User.FirstName ?? "") + " " + (p.Student.User.LastName ?? "")).Trim()
                        : null)
                    .FirstOrDefault(),
                ScheduleId = _context.SessionComplaints
                    .Where(c => c.RefundId == r.Id)
                    .Select(c => (int?)c.CourseScheduleId)
                    .FirstOrDefault(),
                OriginalPaymentAmount = r.Payment.TotalAmount,
                InitiatedByName = r.InitiatedByUserId != null
                    ? _context.Users
                        .Where(u => u.Id == r.InitiatedByUserId)
                        .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim())
                        .FirstOrDefault()
                    : null,
                TransactionKey = "ref-" + r.Id
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.Description = $"Refund to student — {item.Reason}";

        return (items, totalCount);
    }

    public Task<Payment?> GetTrackedPaymentWithRefundsAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        _context.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

    public Task<List<int>> GetRefundablePaymentIdsForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default) =>
        _context.EnrollmentPayments
            .AsNoTracking()
            .Where(ep => ep.EnrollmentParticipant.EnrollmentId == enrollmentId
                         && (ep.Status == PaymentStatus.Succeeded
                             || ep.Payment.Status == PaymentStatus.Succeeded))
            .Select(ep => ep.PaymentId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public Task<List<EnrollmentPayment>> GetEnrollmentPaymentsForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        _context.EnrollmentPayments
            .Where(ep => ep.PaymentId == paymentId)
            .ToListAsync(cancellationToken);

    public async Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await _context.Refunds.AddAsync(refund, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<RefundDetailProjection?> GetDetailProjectionAsync(
        int refundId,
        CancellationToken cancellationToken = default)
    {
        var r = await _context.Refunds
            .AsNoTracking()
            .Where(x => x.Id == refundId)
            .Select(x => new RefundDetailProjection
            {
                Id = x.Id,
                PaymentId = x.PaymentId,
                EnrollmentId = x.EnrollmentId,
                Amount = x.Amount,
                Currency = x.Currency,
                Reason = x.Reason,
                Status = x.Status.ToString(),
                ProviderRefundId = x.ProviderRefundId,
                CreatedAt = x.CreatedAt,
                InitiatedByUserId = x.InitiatedByUserId,
                InitiatedByName = x.InitiatedByUserId != null
                    ? _context.Users
                        .Where(u => u.Id == x.InitiatedByUserId)
                        .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim())
                        .FirstOrDefault()
                    : null,
                PaymentTotal = x.Payment.TotalAmount,
                RefundedTotal = x.Payment.Refunds
                    .Where(rr => rr.Status == RefundStatus.Succeeded)
                    .Sum(rr => rr.Amount),
                CourseTitle = x.Enrollment.Course != null ? x.Enrollment.Course.Title : null,
                PayerName = x.Payment.PayerUser != null
                    ? ((x.Payment.PayerUser.FirstName ?? "") + " " + (x.Payment.PayerUser.LastName ?? "")).Trim()
                    : null,
                TeacherId = x.Enrollment.ApprovedByTeacherId,
                TeacherName = x.Enrollment.ApprovedByTeacher.User != null
                    ? ((x.Enrollment.ApprovedByTeacher.User.FirstName ?? "") + " "
                       + (x.Enrollment.ApprovedByTeacher.User.LastName ?? "")).Trim()
                    : null,
                StudentId = x.Enrollment.Participants.OrderBy(p => p.Id).Select(p => (int?)p.StudentId).FirstOrDefault(),
                StudentName = x.Enrollment.Participants.OrderBy(p => p.Id)
                    .Select(p => p.Student.User != null
                        ? ((p.Student.User.FirstName ?? "") + " " + (p.Student.User.LastName ?? "")).Trim()
                        : null)
                    .FirstOrDefault(),
                ScheduleId = _context.SessionComplaints
                    .Where(c => c.RefundId == x.Id)
                    .Select(c => (int?)c.CourseScheduleId)
                    .FirstOrDefault(),
                PaymentProviderRef = x.Payment.ProviderTransactionId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (r?.ScheduleId != null)
        {
            var schedule = await _context.CourseSchedules.AsNoTracking()
                .Where(s => s.Id == r.ScheduleId)
                .Select(s => new { s.Date, s.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (schedule != null)
                r.SessionLabel = $"Session {schedule.Date:yyyy-MM-dd} (#{schedule.Id})";
        }

        return r;
    }

    public Task<List<ScheduleStatusProjection>> GetScheduleStatusesForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default) =>
        _context.CourseSchedules
            .AsNoTracking()
            .Where(s => s.EnrollmentId == enrollmentId
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .Select(s => new ScheduleStatusProjection
            {
                Id = s.Id,
                Status = s.Status.ToString(),
                Date = s.Date
            })
            .ToListAsync(cancellationToken);

    public Task<List<EarningLineProjection>> GetEarningLinesForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default) =>
        _context.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.EnrollmentId == enrollmentId)
            .Select(l => new EarningLineProjection
            {
                Id = l.Id,
                Status = l.Status.ToString(),
                Amount = l.Amount,
                BatchStatus = l.PayoutItem != null
                    ? l.PayoutItem.PayoutBatch.Status.ToString()
                    : null
            })
            .ToListAsync(cancellationToken);

    public Task<List<TeacherEarningLine>> GetPendingEarningLinesForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default) =>
        _context.TeacherEarningLines
            .Where(l => l.EnrollmentId == enrollmentId
                        && (l.Status == TeacherEarningLineStatus.Pending
                            || l.Status == TeacherEarningLineStatus.OnHold))
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .ToListAsync(cancellationToken);

    public Task<int?> GetComplaintIdForRefundAsync(
        int refundId,
        CancellationToken cancellationToken = default) =>
        _context.SessionComplaints.AsNoTracking()
            .Where(c => c.RefundId == refundId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetTeacherIdForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = await _context.Enrollments.AsNoTracking()
            .Where(e => e.Id == enrollmentId)
            .Select(e => e.ApprovedByTeacherId)
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherId > 0)
            return teacherId;

        return await _context.Enrollments.AsNoTracking()
            .Where(e => e.Id == enrollmentId && e.Course != null)
            .Select(e => e.Course!.TeacherId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
