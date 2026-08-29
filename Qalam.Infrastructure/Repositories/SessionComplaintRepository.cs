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
        Attachments = row.Attachments.Select(a => new AdminSessionComplaintAttachmentDto
        {
            AttachmentId = a.Id,
            FileName = a.FileName,
            FileUrl = a.FileUrl,
            ContentType = a.ContentType,
        }).ToList(),
    };
}
