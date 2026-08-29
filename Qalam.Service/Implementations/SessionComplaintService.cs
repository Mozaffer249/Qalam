using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class SessionComplaintService : ISessionComplaintService
{
    private const long MaxAttachmentSizeBytes = 25 * 1024 * 1024;
    private static readonly string[] AllowedAttachmentExtensions =
        { ".pdf", ".png", ".jpg", ".jpeg", ".webp" };

    private readonly ISessionComplaintRepository _complaints;
    private readonly ICourseScheduleRepository _schedules;
    private readonly ISessionAuditService _audit;
    private readonly ITeacherEarningService _teacherEarning;
    private readonly IRefundService _refundService;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;

    public SessionComplaintService(
        ISessionComplaintRepository complaints,
        ICourseScheduleRepository schedules,
        ISessionAuditService audit,
        ITeacherEarningService teacherEarning,
        IRefundService refundService,
        IFileStorageService fileStorage,
        IConfiguration configuration)
    {
        _complaints = complaints;
        _schedules = schedules;
        _audit = audit;
        _teacherEarning = teacherEarning;
        _refundService = refundService;
        _fileStorage = fileStorage;
        _configuration = configuration;
    }

    public Task<bool> HasBlockingComplaintAsync(int courseScheduleId, CancellationToken cancellationToken = default) =>
        _complaints.HasBlockingComplaintAsync(courseScheduleId, cancellationToken);

    public async Task<SessionComplaint> FileComplaintAsync(
        int courseScheduleId,
        int studentId,
        int userId,
        SessionComplaintReason reasonCode,
        string description,
        IReadOnlyList<IFormFile>? attachments,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _schedules.GetWithParticipantsForComplaintAsync(courseScheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        if (!schedule.Enrollment.Participants.Any(p => p.StudentId == studentId))
            throw new InvalidOperationException("Student is not a participant in this enrollment.");

        if (await _complaints.HasOpenForStudentAsync(courseScheduleId, studentId, cancellationToken))
            throw new InvalidOperationException("An open complaint already exists for this session.");

        var teacherId = schedule.Enrollment.ApprovedByTeacherId > 0
            ? schedule.Enrollment.ApprovedByTeacherId
            : schedule.Enrollment.Course?.TeacherId ?? 0;
        if (teacherId <= 0 && schedule.Enrollment.CourseId.HasValue)
            teacherId = await _schedules.GetCourseTeacherIdAsync(schedule.Enrollment.CourseId.Value, cancellationToken);

        var complaint = new SessionComplaint
        {
            CourseScheduleId = courseScheduleId,
            EnrollmentId = schedule.EnrollmentId,
            StudentId = studentId,
            TeacherId = teacherId,
            ReasonCode = reasonCode,
            Description = description.Trim(),
            Status = SessionComplaintStatus.Open,
            FiledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        await _complaints.AddComplaintAsync(complaint, cancellationToken);

        if (attachments != null)
        {
            foreach (var file in attachments.Where(f => f.Length > 0))
            {
                await SaveAndQueueAttachmentAsync(file, complaint.Id, userId, cancellationToken);
            }
        }

        await HoldEarningForScheduleAsync(courseScheduleId, cancellationToken);

        await _audit.LogAsync(
            courseScheduleId,
            userId,
            "Student",
            SessionAuditActionType.ComplaintFiled,
            new { complaintId = complaint.Id, reasonCode = reasonCode.ToString() },
            cancellationToken);

        return complaint;
    }

    public Task<SessionComplaintDetailDto?> GetComplaintAsync(
        int complaintId,
        int? studentId,
        CancellationToken cancellationToken = default) =>
        _complaints.GetDetailAsync(complaintId, studentId, cancellationToken);

    public Task<List<SessionComplaint>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default) =>
        _complaints.ListForScheduleAsync(courseScheduleId, cancellationToken);

    public async Task AssignAsync(
        int complaintId,
        int adminUserId,
        int assignedToUserId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedComplaintAsync(complaintId, cancellationToken);
        complaint.AssignedToUserId = assignedToUserId;
        complaint.Status = SessionComplaintStatus.InReview;
        await _complaints.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(
            complaint.CourseScheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.ComplaintStatusChanged,
            new { complaintId, status = complaint.Status.ToString(), assignedToUserId },
            cancellationToken);
    }

    public async Task RequestTeacherResponseAsync(
        int complaintId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedComplaintAsync(complaintId, cancellationToken);
        complaint.RequiresTeacherResponse = true;
        complaint.Status = SessionComplaintStatus.AwaitingTeacher;
        await _complaints.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(
            complaint.CourseScheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.ComplaintStatusChanged,
            new { complaintId, status = complaint.Status.ToString() },
            cancellationToken);
    }

    public async Task ResolveAsync(
        int complaintId,
        int adminUserId,
        SessionComplaintResolution resolutionCode,
        string? resolutionNotes,
        decimal? refundAmount,
        int? paymentId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedComplaintAsync(complaintId, cancellationToken);
        complaint.Status = resolutionCode == SessionComplaintResolution.RejectComplaint
            ? SessionComplaintStatus.Rejected
            : SessionComplaintStatus.Resolved;
        complaint.ResolutionCode = resolutionCode;
        complaint.ResolutionNotes = resolutionNotes;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedByUserId = adminUserId;
        complaint.RequiresTeacherResponse = false;
        await _complaints.SaveChangesAsync(cancellationToken);

        if (resolutionCode is SessionComplaintResolution.FullRefund or SessionComplaintResolution.PartialRefund
            && paymentId.HasValue && refundAmount.HasValue && refundAmount.Value > 0)
        {
            await _refundService.IssueRefundAsync(
                paymentId.Value,
                complaint.EnrollmentId,
                refundAmount.Value,
                "SAR",
                resolutionNotes ?? "Session complaint refund",
                adminUserId,
                cancellationToken);
            await _audit.LogAsync(
                complaint.CourseScheduleId,
                adminUserId,
                "Admin",
                SessionAuditActionType.RefundIssued,
                new { complaintId, paymentId, refundAmount },
                cancellationToken);
        }

        if (resolutionCode == SessionComplaintResolution.DeductTeacherEarning)
            await VoidEarningForScheduleAsync(complaint.CourseScheduleId, cancellationToken);
        else if (resolutionCode is SessionComplaintResolution.RejectComplaint or SessionComplaintResolution.NoAction)
            await ReleaseEarningForScheduleAsync(complaint.CourseScheduleId, cancellationToken);
        else if (resolutionCode is SessionComplaintResolution.FullRefund or SessionComplaintResolution.PartialRefund)
            await VoidEarningForScheduleAsync(complaint.CourseScheduleId, cancellationToken);
        else
            await ReleaseEarningForScheduleAsync(complaint.CourseScheduleId, cancellationToken);

        await _audit.LogAsync(
            complaint.CourseScheduleId,
            adminUserId,
            "Admin",
            SessionAuditActionType.ComplaintStatusChanged,
            new { complaintId, resolutionCode = resolutionCode.ToString() },
            cancellationToken);
    }

    public async Task RespondAsTeacherAsync(
        int complaintId,
        int teacherId,
        string response,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _complaints.GetByIdForTeacherTrackedAsync(complaintId, teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Complaint not found.");

        if (!complaint.RequiresTeacherResponse)
            throw new InvalidOperationException("Teacher response is not requested for this complaint.");

        complaint.TeacherResponse = response.Trim();
        complaint.TeacherRespondedAt = DateTime.UtcNow;
        complaint.Status = SessionComplaintStatus.InReview;
        complaint.RequiresTeacherResponse = false;
        await _complaints.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            complaint.CourseScheduleId,
            teacherId,
            "Teacher",
            SessionAuditActionType.ComplaintStatusChanged,
            new { complaintId, action = "TeacherResponded" },
            cancellationToken);
    }

    public async Task HoldEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default)
    {
        var line = await _complaints.GetActiveEarningLineForScheduleAsync(courseScheduleId, cancellationToken);
        if (line == null)
        {
            if (await _schedules.IsCompletedAsync(courseScheduleId, cancellationToken))
                await _teacherEarning.AccrueForCompletedScheduleAsync(
                    courseScheduleId,
                    TeacherEarningLineStatus.OnHold,
                    cancellationToken);
            return;
        }

        if (line.Status == TeacherEarningLineStatus.Pending)
        {
            line.Status = TeacherEarningLineStatus.OnHold;
            await _complaints.UpdateEarningLineAsync(line, cancellationToken);
        }
    }

    public async Task ReleaseEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default)
    {
        if (await HasBlockingComplaintAsync(courseScheduleId, cancellationToken))
            return;

        var line = await _complaints.GetOnHoldEarningLineForScheduleAsync(courseScheduleId, cancellationToken);
        if (line == null)
            return;

        line.Status = TeacherEarningLineStatus.Pending;
        await _complaints.UpdateEarningLineAsync(line, cancellationToken);
    }

    public async Task VoidEarningForScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default)
    {
        var line = await _complaints.GetActiveEarningLineForScheduleAsync(courseScheduleId, cancellationToken);
        if (line == null)
            return;

        line.Status = TeacherEarningLineStatus.Voided;
        await _complaints.UpdateEarningLineAsync(line, cancellationToken);
    }

    private async Task<SessionComplaint> GetTrackedComplaintAsync(int complaintId, CancellationToken cancellationToken) =>
        await _complaints.GetByIdTrackedAsync(complaintId, cancellationToken)
        ?? throw new InvalidOperationException("Complaint not found.");

    private async Task SaveAndQueueAttachmentAsync(
        IFormFile file,
        int complaintId,
        int userId,
        CancellationToken cancellationToken)
    {
        if (!await _fileStorage.ValidateFileAsync(file, AllowedAttachmentExtensions, MaxAttachmentSizeBytes))
            throw new InvalidOperationException(
                "Invalid attachment — allowed: PDF/PNG/JPG/WEBP, max 25 MB.");

        var attachment = new SessionComplaintAttachment
        {
            ComplaintId = complaintId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileUrl = "pending",
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        await _complaints.AddAttachmentAsync(attachment, cancellationToken);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".bin";

        var storageKey = $"session-complaints/{complaintId}/{attachment.Id}{ext}";
        var ossPublicBase = _configuration["OssSettings:LearningPublicBaseUrl"]
                          ?? _configuration["OSS_LEARNING_PUBLIC_BASE_URL"]
                          ?? _configuration["OssSettings:PublicBaseUrl"]
                          ?? _configuration["OSS_PUBLIC_BASE_URL"]
                          ?? string.Empty;

        if (string.IsNullOrWhiteSpace(ossPublicBase))
            throw new InvalidOperationException(
                "OssSettings:LearningPublicBaseUrl is not configured; cannot upload complaint attachments.");

        attachment.FileUrl = $"{ossPublicBase.TrimEnd('/')}/{storageKey}";
        await _complaints.SaveChangesAsync(cancellationToken);

        try
        {
            await _fileStorage.QueueSessionComplaintAttachmentUploadAsync(
                file, complaintId, attachment.Id, storageKey);
        }
        catch
        {
            await _complaints.RemoveAttachmentAsync(attachment.Id, cancellationToken);
            throw;
        }
    }
}
