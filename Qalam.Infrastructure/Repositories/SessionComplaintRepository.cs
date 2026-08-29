using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class SessionComplaintRepository : ISessionComplaintRepository
{
    private static readonly SessionComplaintStatus[] BlockingStatuses =
    [
        SessionComplaintStatus.Open,
        SessionComplaintStatus.InReview,
        SessionComplaintStatus.AwaitingTeacher,
        SessionComplaintStatus.AwaitingStudent,
    ];

    private readonly ApplicationDBContext _context;

    public SessionComplaintRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public Task<bool> HasBlockingComplaintAsync(int courseScheduleId, CancellationToken cancellationToken = default) =>
        _context.SessionComplaints.AsNoTracking()
            .AnyAsync(c => c.CourseScheduleId == courseScheduleId && BlockingStatuses.Contains(c.Status),
                cancellationToken);

    public Task<bool> HasOpenForStudentAsync(
        int courseScheduleId,
        int studentId,
        CancellationToken cancellationToken = default) =>
        _context.SessionComplaints.AnyAsync(
            c => c.CourseScheduleId == courseScheduleId
                 && c.StudentId == studentId
                 && BlockingStatuses.Contains(c.Status),
            cancellationToken);

    public Task<SessionComplaint?> GetByIdTrackedAsync(int complaintId, CancellationToken cancellationToken = default) =>
        _context.SessionComplaints.FirstOrDefaultAsync(c => c.Id == complaintId, cancellationToken);

    public Task<SessionComplaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default) =>
        _context.SessionComplaints.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == complaintId, cancellationToken);

    public Task<bool> BelongsToScheduleAsync(
        int complaintId,
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _context.SessionComplaints.AsNoTracking()
            .AnyAsync(c => c.Id == complaintId && c.CourseScheduleId == courseScheduleId, cancellationToken);

    public async Task<ComplaintSessionFinancialContextDto?> LoadFinancialContextAsync(
        int enrollmentId,
        int courseScheduleId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.PricingSnapshot)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);
        if (enrollment == null)
            return null;

        var schedule = await _context.CourseSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == courseScheduleId, cancellationToken);
        if (schedule == null)
            return null;

        var siblingSchedules = await _context.CourseSchedules
            .AsNoTracking()
            .Where(s => s.EnrollmentId == enrollment.Id
                        && s.Status != ScheduleStatus.Cancelled
                        && s.Status != ScheduleStatus.Rescheduled)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.DurationMinutes })
            .ToListAsync(cancellationToken);

        var totalMinutes = enrollment.PricingSnapshot?.TotalMinutes ?? 0;
        if (totalMinutes <= 0)
            totalMinutes = siblingSchedules.Sum(s => s.DurationMinutes);

        var earnableMinutes = totalMinutes;
        if (enrollment.IsFreeTrial && siblingSchedules.Count > 0)
        {
            var freeMinutes = ResolveFirstSessionMinutes(
                siblingSchedules[0].DurationMinutes > 0 ? siblingSchedules[0].DurationMinutes : null,
                enrollment.Course?.SessionDurationMinutes,
                totalMinutes > 0 ? totalMinutes : null,
                siblingSchedules.Count);
            if (freeMinutes <= 0)
                freeMinutes = 60;
            earnableMinutes = Math.Max(0, totalMinutes - freeMinutes);
        }

        var paymentRow = await _context.EnrollmentPayments
            .AsNoTracking()
            .Include(ep => ep.Payment)
                .ThenInclude(p => p.Refunds)
            .Where(ep => ep.EnrollmentParticipant.EnrollmentId == enrollment.Id
                         && (ep.Status == PaymentStatus.Succeeded
                             || ep.Payment.Status == PaymentStatus.Succeeded))
            .OrderByDescending(ep => ep.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var payment = paymentRow?.Payment;
        var alreadyRefunded = payment?.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount) ?? 0m;
        var paymentTotal = payment?.TotalAmount ?? 0m;
        var remaining = Math.Max(0m, paymentTotal - alreadyRefunded);

        var earningLine = await _context.TeacherEarningLines
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.CourseScheduleId == courseScheduleId && l.Status != TeacherEarningLineStatus.Voided,
                cancellationToken);

        return new ComplaintSessionFinancialContextDto
        {
            AmountDue = enrollment.AmountDue,
            SessionDurationMinutes = schedule.DurationMinutes,
            EarnablePackageMinutes = earnableMinutes,
            Currency = enrollment.PricingSnapshot?.Currency ?? payment?.Currency ?? "SAR",
            PrimaryPaymentId = payment?.Id,
            RemainingRefundable = remaining,
            PaymentTotal = paymentTotal,
            SessionEarningAmount = earningLine?.Amount,
            SessionEarningStatus = earningLine?.Status.ToString(),
        };
    }

    public async Task<string> GetPayoutImpactAsync(
        int enrollmentId,
        decimal refundAmount,
        CancellationToken cancellationToken = default)
    {
        var lines = await _context.TeacherEarningLines
            .AsNoTracking()
            .Where(l => l.EnrollmentId == enrollmentId)
            .Select(l => new
            {
                l.Status,
                BatchStatus = l.PayoutItem != null
                    ? (PayoutBatchStatus?)l.PayoutItem.PayoutBatch.Status
                    : null,
            })
            .ToListAsync(cancellationToken);

        var hasPaid = lines.Any(l =>
            l.Status == TeacherEarningLineStatus.IncludedInPayout
            && l.BatchStatus == PayoutBatchStatus.Paid);

        if (hasPaid)
            return "AlreadyPaid";

        if (refundAmount > 0 && lines.Any(l =>
                l.Status is TeacherEarningLineStatus.Pending or TeacherEarningLineStatus.OnHold))
            return "VoidedPending";

        return "None";
    }

    public Task<SessionComplaint?> GetByIdForTeacherTrackedAsync(
        int complaintId,
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _context.SessionComplaints
            .FirstOrDefaultAsync(c => c.Id == complaintId && c.TeacherId == teacherId, cancellationToken);

    public async Task<SessionComplaintDetailDto?> GetDetailAsync(
        int complaintId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        var q = _context.SessionComplaints.AsNoTracking().Where(c => c.Id == complaintId);
        if (studentId.HasValue)
            q = q.Where(c => c.StudentId == studentId.Value);

        var row = await q
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(cancellationToken);
        return row == null ? null : MapComplaint(row);
    }

    public Task<List<SessionComplaint>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _context.SessionComplaints
            .AsNoTracking()
            .Include(c => c.Attachments)
            .Where(c => c.CourseScheduleId == courseScheduleId)
            .OrderByDescending(c => c.FiledAt)
            .ToListAsync(cancellationToken);

    public async Task AddComplaintAsync(SessionComplaint complaint, CancellationToken cancellationToken = default)
    {
        _context.SessionComplaints.Add(complaint);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAttachmentAsync(SessionComplaintAttachment attachment, CancellationToken cancellationToken = default)
    {
        _context.SessionComplaintAttachments.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var row = await _context.SessionComplaintAttachments.FindAsync([attachmentId], cancellationToken);
        if (row == null)
            return;
        _context.SessionComplaintAttachments.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<TeacherEarningLine?> GetActiveEarningLineForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _context.TeacherEarningLines
            .FirstOrDefaultAsync(l => l.CourseScheduleId == courseScheduleId
                                      && l.Status != TeacherEarningLineStatus.Voided,
                cancellationToken);

    public Task<TeacherEarningLine?> GetOnHoldEarningLineForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _context.TeacherEarningLines
            .FirstOrDefaultAsync(l => l.CourseScheduleId == courseScheduleId
                                      && l.Status == TeacherEarningLineStatus.OnHold,
                cancellationToken);

    public async Task UpdateEarningLineAsync(TeacherEarningLine line, CancellationToken cancellationToken = default)
    {
        _context.TeacherEarningLines.Update(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static SessionComplaintDetailDto MapComplaint(SessionComplaint row) => new()
    {
        ComplaintId = row.Id,
        CourseScheduleId = row.CourseScheduleId,
        EnrollmentId = row.EnrollmentId,
        ReasonCode = row.ReasonCode.ToString(),
        Description = row.Description,
        Status = row.Status.ToString(),
        FiledAt = row.FiledAt,
        ResolutionCode = row.ResolutionCode?.ToString(),
        ResolutionNotes = row.ResolutionNotes,
        RequiresTeacherResponse = row.RequiresTeacherResponse,
        TeacherResponse = row.TeacherResponse,
        RefundId = row.RefundId,
        ReplacementScheduleId = row.ReplacementScheduleId,
        Attachments = row.Attachments.Select(a => new AdminSessionComplaintAttachmentDto
        {
            AttachmentId = a.Id,
            FileName = a.FileName,
            FileUrl = a.FileUrl,
            ContentType = a.ContentType,
        }).ToList(),
    };

    private static int ResolveFirstSessionMinutes(
        int? firstSessionDurationMinutes,
        int? sessionDurationMinutes,
        int? totalMinutes,
        int? sessionCount)
    {
        if (firstSessionDurationMinutes is > 0)
            return firstSessionDurationMinutes.Value;
        if (sessionDurationMinutes is > 0)
            return sessionDurationMinutes.Value;
        if (totalMinutes is > 0 && sessionCount is > 0)
            return totalMinutes.Value / sessionCount.Value;
        return 0;
    }
}
