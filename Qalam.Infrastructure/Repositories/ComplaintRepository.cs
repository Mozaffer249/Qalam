using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Complaint;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class ComplaintRepository : IComplaintRepository
{
    private readonly ApplicationDBContext _context;

    public ComplaintRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public Task<Complaint?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Complaints.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Complaint?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Complaints.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Complaint?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Complaints.AsNoTracking()
            .Include(c => c.Attachments)
            .Include(c => c.Timeline)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<(List<Complaint> Items, int Total)> ListAsync(
        ComplaintListFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.Complaints.AsNoTracking(), filter);
        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);
        var items = await query
            .OrderByDescending(c => c.FiledAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<ComplaintCountsDto> GetCountsAsync(
        ComplaintListFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.Complaints.AsNoTracking(), filter ?? new ComplaintListFilter());
        var rows = await query
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(ComplaintStatus status) => rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;

        var dto = new ComplaintCountsDto
        {
            Submitted = CountOf(ComplaintStatus.Submitted),
            InReview = CountOf(ComplaintStatus.InReview),
            AwaitingComplainant = CountOf(ComplaintStatus.AwaitingComplainant),
            AwaitingRespondent = CountOf(ComplaintStatus.AwaitingRespondent),
            DecisionPending = CountOf(ComplaintStatus.DecisionPending),
        };
        dto.OpenTotal = dto.Submitted + dto.InReview + dto.AwaitingComplainant
                        + dto.AwaitingRespondent + dto.DecisionPending;
        return dto;
    }

    public Task<bool> HasOpenForSubjectAsync(
        ComplaintSubjectType subjectType,
        int complainantUserId,
        int? courseScheduleId,
        int? enrollmentId,
        int? paymentId,
        int? refundId,
        int? openSessionRequestId,
        CancellationToken cancellationToken = default)
    {
        var open = new[]
        {
            ComplaintStatus.Submitted,
            ComplaintStatus.InReview,
            ComplaintStatus.AwaitingComplainant,
            ComplaintStatus.AwaitingRespondent,
            ComplaintStatus.DecisionPending,
        };

        return _context.Complaints.AsNoTracking().AnyAsync(c =>
            c.ComplainantUserId == complainantUserId
            && c.SubjectType == subjectType
            && open.Contains(c.Status)
            && (subjectType != ComplaintSubjectType.Session || c.CourseScheduleId == courseScheduleId)
            && (subjectType != ComplaintSubjectType.Enrollment || c.EnrollmentId == enrollmentId)
            && (subjectType != ComplaintSubjectType.Payment || c.PaymentId == paymentId)
            && (subjectType != ComplaintSubjectType.Refund || c.RefundId == refundId)
            && (subjectType != ComplaintSubjectType.OpenSessionRequest || c.OpenSessionRequestId == openSessionRequestId),
            cancellationToken);
    }

    public Task<int> CountOpenForPartiesAsync(
        int complainantUserId,
        int? respondentTeacherId,
        CancellationToken cancellationToken = default)
    {
        var open = new[]
        {
            ComplaintStatus.Submitted,
            ComplaintStatus.InReview,
            ComplaintStatus.AwaitingComplainant,
            ComplaintStatus.AwaitingRespondent,
            ComplaintStatus.DecisionPending,
        };

        return _context.Complaints.AsNoTracking().CountAsync(c =>
            open.Contains(c.Status)
            && (c.ComplainantUserId == complainantUserId
                || (respondentTeacherId.HasValue && c.RespondentTeacherId == respondentTeacherId)),
            cancellationToken);
    }

    public async Task AddAsync(Complaint complaint, CancellationToken cancellationToken = default)
    {
        await _context.Complaints.AddAsync(complaint, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTimelineAsync(ComplaintTimelineEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.ComplaintTimelineEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAttachmentAsync(ComplaintAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _context.ComplaintAttachments.AddAsync(attachment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ComplaintAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (entity == null)
            return;
        _context.ComplaintAttachments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<Complaint> ApplyFilter(IQueryable<Complaint> query, ComplaintListFilter filter)
    {
        if (filter.Status.HasValue)
            query = query.Where(c => c.Status == filter.Status);
        if (filter.SubjectType.HasValue)
            query = query.Where(c => c.SubjectType == filter.SubjectType);
        if (filter.Priority.HasValue)
            query = query.Where(c => c.Priority == filter.Priority);
        if (filter.AssignedToUserId.HasValue)
            query = query.Where(c => c.AssignedToUserId == filter.AssignedToUserId);
        if (filter.ComplainantUserId.HasValue)
            query = query.Where(c => c.ComplainantUserId == filter.ComplainantUserId);
        if (filter.TeacherId.HasValue)
            query = query.Where(c => c.RespondentTeacherId == filter.TeacherId);
        if (filter.StudentId.HasValue)
            query = query.Where(c => c.AffectedStudentId == filter.StudentId);
        if (filter.FromUtc.HasValue)
            query = query.Where(c => c.FiledAt >= filter.FromUtc);
        if (filter.ToUtc.HasValue)
            query = query.Where(c => c.FiledAt <= filter.ToUtc);

        if (string.Equals(filter.Scope, "open", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c =>
                c.Status == ComplaintStatus.Submitted
                || c.Status == ComplaintStatus.InReview
                || c.Status == ComplaintStatus.AwaitingComplainant
                || c.Status == ComplaintStatus.AwaitingRespondent
                || c.Status == ComplaintStatus.DecisionPending);
        }

        return query;
    }
}
