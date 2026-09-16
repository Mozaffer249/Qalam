using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Complaint;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ComplaintService : IComplaintService
{
    private const long MaxAttachmentSizeBytes = 25 * 1024 * 1024;
    private static readonly string[] AllowedAttachmentExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".webp" };

    private readonly IComplaintRepository _complaints;
    private readonly ApplicationDBContext _db;
    private readonly ISessionComplaintService _sessionComplaints;
    private readonly IComplaintResolutionOrchestrator _resolution;
    private readonly IFileStorageService _fileStorage;
    private readonly IStoragePublicUrlProvider _storagePublicUrls;

    public ComplaintService(
        IComplaintRepository complaints,
        ApplicationDBContext db,
        ISessionComplaintService sessionComplaints,
        IComplaintResolutionOrchestrator resolution,
        IFileStorageService fileStorage,
        IStoragePublicUrlProvider storagePublicUrls)
    {
        _complaints = complaints;
        _db = db;
        _sessionComplaints = sessionComplaints;
        _resolution = resolution;
        _fileStorage = fileStorage;
        _storagePublicUrls = storagePublicUrls;
    }

    public async Task<(List<ComplaintListItemDto> Items, int Total)> ListAsync(
        ComplaintListFilter filter,
        int pageNumber,
        int pageSize,
        string viewerRole,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _complaints.ListAsync(filter, pageNumber, pageSize, cancellationToken);
        var dtos = new List<ComplaintListItemDto>(items.Count);
        foreach (var c in items)
            dtos.Add(await MapListItemAsync(c, viewerRole, cancellationToken));
        return (dtos, total);
    }

    public async Task<ComplaintDetailDto?> GetAsync(
        int complaintId,
        int? viewerUserId,
        string viewerRole,
        int? teacherId = null,
        CancellationToken cancellationToken = default)
    {
        var c = await _complaints.GetDetailAsync(complaintId, cancellationToken);
        if (c == null)
            return null;

        if (!CanView(c, viewerUserId, viewerRole, teacherId))
            return null;

        return await MapDetailAsync(c, viewerRole, cancellationToken);
    }

    public Task<ComplaintCountsDto> GetCountsAsync(
        ComplaintListFilter? filter = null,
        CancellationToken cancellationToken = default) =>
        _complaints.GetCountsAsync(filter, cancellationToken);

    public async Task<ComplaintDetailDto> FileAsync(
        int complainantUserId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        IReadOnlyList<IFormFile>? attachments,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new InvalidOperationException("Description is required.");

        if (!ComplaintRules.SubjectMatchesLinks(
                request.SubjectType,
                request.CourseScheduleId,
                request.EnrollmentId,
                request.PaymentId,
                request.RefundId,
                request.OpenSessionRequestId))
            throw new InvalidOperationException("Subject links do not match subject type.");

        var ownership = await ResolveOwnershipAsync(complainantUserId, role, request, cancellationToken);

        if (request.SubjectType != ComplaintSubjectType.Other
            && await _complaints.HasOpenForSubjectAsync(
                request.SubjectType,
                complainantUserId,
                request.CourseScheduleId,
                request.EnrollmentId,
                request.PaymentId,
                request.RefundId,
                request.OpenSessionRequestId,
                cancellationToken))
            throw new InvalidOperationException("An open complaint already exists for this subject.");

        int? legacySessionComplaintId = null;
        if (request.SubjectType == ComplaintSubjectType.Session)
        {
            var studentId = ownership.AffectedStudentId
                ?? throw new InvalidOperationException("Affected student is required for session complaints.");
            var sessionComplaint = await _sessionComplaints.FileComplaintAsync(
                request.CourseScheduleId!.Value,
                studentId,
                complainantUserId,
                ComplaintRules.ToLegacySessionReason(request.ReasonCode),
                request.Description,
                attachments,
                cancellationToken);
            legacySessionComplaintId = sessionComplaint.Id;

            // SessionComplaintService already held earnings; mirror into unified table.
            var existing = await _db.Complaints.AsNoTracking()
                .FirstOrDefaultAsync(c => c.LegacySessionComplaintId == sessionComplaint.Id, cancellationToken);
            if (existing != null)
                return (await GetAsync(existing.Id, complainantUserId, role.ToString(), null, cancellationToken))!;
        }

        var priorOpen = await _complaints.CountOpenForPartiesAsync(
            complainantUserId, ownership.RespondentTeacherId, cancellationToken);

        var now = DateTime.UtcNow;
        var complaint = new Complaint
        {
            SubjectType = request.SubjectType,
            CourseScheduleId = request.CourseScheduleId ?? ownership.CourseScheduleId,
            EnrollmentId = request.EnrollmentId ?? ownership.EnrollmentId,
            PaymentId = request.PaymentId,
            RefundId = request.RefundId,
            OpenSessionRequestId = request.OpenSessionRequestId,
            ComplainantUserId = complainantUserId,
            ComplainantRole = role,
            AffectedStudentId = ownership.AffectedStudentId,
            RespondentTeacherId = ownership.RespondentTeacherId,
            RespondentUserId = ownership.RespondentUserId,
            ReasonCode = request.ReasonCode,
            Description = request.Description.Trim(),
            Status = ComplaintStatus.Submitted,
            Priority = ComplaintRules.SuggestPriority(
                request.SubjectType, ownership.RelatedAmount, ownership.SessionStartsAtUtc, priorOpen),
            FiledAt = now,
            CreatedAt = now,
            LegacySessionComplaintId = legacySessionComplaintId,
        };

        if (legacySessionComplaintId.HasValue)
        {
            var mirror = await EnsureSessionMirrorAsync(legacySessionComplaintId.Value, complaint, cancellationToken);
            await CopyLegacyAttachmentsAsync(legacySessionComplaintId.Value, mirror.Id, cancellationToken);
            await AddTimelineAsync(
                mirror.Id,
                ComplaintTimelineEventType.Filed,
                null,
                ComplaintStatus.Submitted,
                complainantUserId,
                role.ToString(),
                "Complaint filed",
                cancellationToken);

            return (await GetAsync(mirror.Id, complainantUserId, role.ToString(), null, cancellationToken))!;
        }

        await _complaints.AddAsync(complaint, cancellationToken);

        if (attachments != null)
        {
            foreach (var file in attachments.Where(f => f.Length > 0))
                await SaveAttachmentAsync(file, complaint.Id, complainantUserId, cancellationToken);
        }

        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.Filed,
            null,
            ComplaintStatus.Submitted,
            complainantUserId,
            role.ToString(),
            "Complaint filed",
            cancellationToken);

        return (await GetAsync(complaint.Id, complainantUserId, role.ToString(), null, cancellationToken))!;
    }

    public async Task AssignAsync(
        int complaintId,
        int adminUserId,
        int assignedToUserId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        var from = complaint.Status;
        complaint.AssignedToUserId = assignedToUserId;
        if (complaint.Status == ComplaintStatus.Submitted)
        {
            ComplaintRules.EnsureTransition(from, ComplaintStatus.InReview);
            complaint.Status = ComplaintStatus.InReview;
        }

        await _complaints.SaveChangesAsync(cancellationToken);
        await SyncLegacySessionAsync(complaint, cancellationToken);
        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.Assigned,
            from,
            complaint.Status,
            adminUserId,
            "Admin",
            $"Assigned to user {assignedToUserId}",
            cancellationToken);
    }

    public async Task SetPriorityAsync(
        int complaintId,
        int adminUserId,
        ComplaintPriority priority,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        var previous = complaint.Priority;
        complaint.Priority = priority;
        await _complaints.SaveChangesAsync(cancellationToken);
        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.PriorityChanged,
            complaint.Status,
            complaint.Status,
            adminUserId,
            "Admin",
            $"Priority {previous} → {priority}",
            cancellationToken);
    }

    public async Task RequestInfoAsync(
        int complaintId,
        int adminUserId,
        string target,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        var from = complaint.Status;
        var normalized = target.Trim();
        ComplaintStatus to;
        if (normalized.Equals("Complainant", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            to = ComplaintStatus.AwaitingComplainant;
            complaint.RequiresComplainantResponse = true;
        }
        else if (normalized.Equals("Respondent", StringComparison.OrdinalIgnoreCase)
                 || normalized.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
        {
            to = ComplaintStatus.AwaitingRespondent;
            complaint.RequiresRespondentResponse = true;
        }
        else
            throw new InvalidOperationException("Target must be Complainant or Respondent.");

        if (from is ComplaintStatus.Submitted)
        {
            ComplaintRules.EnsureTransition(from, ComplaintStatus.InReview);
            complaint.Status = ComplaintStatus.InReview;
            from = ComplaintStatus.InReview;
        }

        ComplaintRules.EnsureTransition(from, to);
        complaint.Status = to;
        await _complaints.SaveChangesAsync(cancellationToken);
        await SyncLegacySessionAsync(complaint, cancellationToken);
        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.InfoRequested,
            from,
            to,
            adminUserId,
            "Admin",
            notes,
            cancellationToken);
    }

    public async Task AdvanceAsync(
        int complaintId,
        int adminUserId,
        ComplaintStatus? toStatus,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        var from = complaint.Status;
        var to = toStatus ?? (from == ComplaintStatus.Submitted
            ? ComplaintStatus.InReview
            : ComplaintStatus.DecisionPending);

        ComplaintRules.EnsureTransition(from, to);
        complaint.Status = to;
        await _complaints.SaveChangesAsync(cancellationToken);
        await SyncLegacySessionAsync(complaint, cancellationToken);
        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.StatusChanged,
            from,
            to,
            adminUserId,
            "Admin",
            null,
            cancellationToken);
    }

    public async Task CancelAsync(
        int complaintId,
        int adminUserId,
        string notes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new InvalidOperationException("Cancellation reason is required.");

        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        if (!ComplaintRules.IsOpen(complaint.Status))
            throw new InvalidOperationException("Complaint is already closed.");

        var from = complaint.Status;
        ComplaintRules.EnsureTransition(from, ComplaintStatus.Cancelled);
        complaint.Status = ComplaintStatus.Cancelled;
        complaint.ResolutionCode = ComplaintResolution.CancelledByAdmin;
        complaint.ResolutionNotes = notes.Trim();
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.ResolvedByUserId = adminUserId;
        complaint.RequiresComplainantResponse = false;
        complaint.RequiresRespondentResponse = false;
        await _complaints.SaveChangesAsync(cancellationToken);
        await SyncLegacySessionAsync(complaint, cancellationToken);

        if (complaint.CourseScheduleId.HasValue)
            await _sessionComplaints.ReleaseEarningForScheduleAsync(complaint.CourseScheduleId.Value, cancellationToken);

        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.Cancelled,
            from,
            ComplaintStatus.Cancelled,
            adminUserId,
            "Admin",
            notes,
            cancellationToken);
    }

    public async Task RespondAsComplainantAsync(
        int complaintId,
        int userId,
        string response,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("Response is required.");

        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        if (complaint.ComplainantUserId != userId)
            throw new InvalidOperationException("Only the complainant can respond.");
        if (complaint.Status != ComplaintStatus.AwaitingComplainant && !complaint.RequiresComplainantResponse)
            throw new InvalidOperationException("Complainant response is not requested.");

        var from = complaint.Status;
        complaint.ComplainantResponse = response.Trim();
        complaint.ComplainantRespondedAt = DateTime.UtcNow;
        complaint.RequiresComplainantResponse = false;
        ComplaintRules.EnsureTransition(from, ComplaintStatus.InReview);
        complaint.Status = ComplaintStatus.InReview;
        await _complaints.SaveChangesAsync(cancellationToken);
        await SyncLegacySessionAsync(complaint, cancellationToken);
        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.ComplainantResponded,
            from,
            ComplaintStatus.InReview,
            userId,
            "Complainant",
            response.Trim(),
            cancellationToken);
    }

    public async Task RespondAsRespondentAsync(
        int complaintId,
        int userId,
        int? teacherId,
        string response,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("Response is required.");

        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        var isTeacher = teacherId.HasValue && complaint.RespondentTeacherId == teacherId;
        var isRespondentUser = complaint.RespondentUserId == userId;
        if (!isTeacher && !isRespondentUser)
            throw new InvalidOperationException("Only the respondent can respond.");
        if (complaint.Status != ComplaintStatus.AwaitingRespondent && !complaint.RequiresRespondentResponse)
            throw new InvalidOperationException("Respondent response is not requested.");

        var from = complaint.Status;
        complaint.RespondentResponse = response.Trim();
        complaint.RespondentRespondedAt = DateTime.UtcNow;
        complaint.RequiresRespondentResponse = false;
        ComplaintRules.EnsureTransition(from, ComplaintStatus.InReview);
        complaint.Status = ComplaintStatus.InReview;
        await _complaints.SaveChangesAsync(cancellationToken);
        await SyncLegacySessionAsync(complaint, cancellationToken);

        if (complaint.LegacySessionComplaintId.HasValue && teacherId.HasValue)
        {
            try
            {
                await _sessionComplaints.RespondAsTeacherAsync(
                    complaint.LegacySessionComplaintId.Value,
                    teacherId.Value,
                    response,
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Unified row is source of truth if legacy already updated.
            }
        }

        await AddTimelineAsync(
            complaint.Id,
            ComplaintTimelineEventType.RespondentResponded,
            from,
            ComplaintStatus.InReview,
            userId,
            "Respondent",
            response.Trim(),
            cancellationToken);
    }

    public async Task<ComplaintResolvePreviewDto> GetResolvePreviewAsync(
        int complaintId,
        ComplaintResolution resolutionCode,
        decimal? refundAmount,
        int? paymentId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _complaints.GetByIdAsync(complaintId, cancellationToken)
            ?? throw new InvalidOperationException("Complaint not found.");

        return await _resolution.GetPreviewUnifiedAsync(
            complaint,
            resolutionCode,
            refundAmount,
            paymentId,
            cancellationToken);
    }

    public async Task ResolveAsync(
        int complaintId,
        int adminUserId,
        ResolveComplaintRequest request,
        CancellationToken cancellationToken = default)
    {
        var complaint = await GetTrackedAsync(complaintId, cancellationToken);
        if (!ComplaintRules.IsOpen(complaint.Status))
            throw new InvalidOperationException("Complaint is already closed.");

        if (complaint.Status is not ComplaintStatus.DecisionPending and not ComplaintStatus.InReview
            and not ComplaintStatus.AwaitingComplainant and not ComplaintStatus.AwaitingRespondent
            and not ComplaintStatus.Submitted)
            throw new InvalidOperationException("Complaint cannot be resolved from the current status.");

        // Move to DecisionPending if needed, then resolve.
        if (complaint.Status != ComplaintStatus.DecisionPending
            && ComplaintRules.CanTransition(complaint.Status, ComplaintStatus.DecisionPending))
        {
            var from = complaint.Status;
            complaint.Status = ComplaintStatus.DecisionPending;
            await _complaints.SaveChangesAsync(cancellationToken);
            await AddTimelineAsync(
                complaint.Id,
                ComplaintTimelineEventType.StatusChanged,
                from,
                ComplaintStatus.DecisionPending,
                adminUserId,
                "Admin",
                "Advanced to decision pending",
                cancellationToken);
        }

        await _resolution.ResolveUnifiedAsync(complaint, adminUserId, request, cancellationToken);

        // Reload after orchestrator mutations
        var updated = await GetTrackedAsync(complaintId, cancellationToken);
        await SyncLegacySessionAsync(updated, cancellationToken);
    }

    private async Task<Complaint> EnsureSessionMirrorAsync(
        int legacyId,
        Complaint template,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Complaints
            .FirstOrDefaultAsync(c => c.LegacySessionComplaintId == legacyId, cancellationToken);
        if (existing != null)
            return existing;

        template.LegacySessionComplaintId = legacyId;
        template.Id = 0;
        await _complaints.AddAsync(template, cancellationToken);
        return template;
    }

    private async Task CopyLegacyAttachmentsAsync(
        int legacyComplaintId,
        int unifiedComplaintId,
        CancellationToken cancellationToken)
    {
        var already = await _db.ComplaintAttachments.AsNoTracking()
            .AnyAsync(a => a.ComplaintId == unifiedComplaintId, cancellationToken);
        if (already)
            return;

        var legacyAttachments = await _db.SessionComplaintAttachments.AsNoTracking()
            .Where(a => a.ComplaintId == legacyComplaintId)
            .ToListAsync(cancellationToken);

        foreach (var a in legacyAttachments)
        {
            await _complaints.AddAttachmentAsync(new ComplaintAttachment
            {
                ComplaintId = unifiedComplaintId,
                FileUrl = a.FileUrl,
                FileName = a.FileName,
                ContentType = a.ContentType,
                UploadedByUserId = a.UploadedByUserId,
                UploadedAt = a.UploadedAt,
                CreatedAt = a.CreatedAt,
            }, cancellationToken);
        }
    }

    private async Task SyncLegacySessionAsync(Complaint complaint, CancellationToken cancellationToken)
    {
        if (complaint.SubjectType != ComplaintSubjectType.Session || !complaint.LegacySessionComplaintId.HasValue)
            return;

        var legacy = await _db.SessionComplaints
            .FirstOrDefaultAsync(c => c.Id == complaint.LegacySessionComplaintId.Value, cancellationToken);
        if (legacy == null)
            return;

        legacy.Status = ComplaintRules.ToLegacySessionStatus(complaint.Status);
        legacy.AssignedToUserId = complaint.AssignedToUserId;
        legacy.RequiresTeacherResponse = complaint.RequiresRespondentResponse;
        legacy.TeacherResponse = complaint.RespondentResponse;
        legacy.TeacherRespondedAt = complaint.RespondentRespondedAt;
        legacy.ResolutionNotes = complaint.ResolutionNotes;
        legacy.ResolvedAt = complaint.ResolvedAt;
        legacy.ResolvedByUserId = complaint.ResolvedByUserId;
        legacy.RefundId = complaint.LinkedRefundId;
        legacy.ReplacementScheduleId = complaint.ReplacementScheduleId;
        if (complaint.ResolutionCode.HasValue)
            legacy.ResolutionCode = ComplaintRules.ToLegacySessionResolution(complaint.ResolutionCode.Value);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<OwnershipContext> ResolveOwnershipAsync(
        int userId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        return request.SubjectType switch
        {
            ComplaintSubjectType.Session => await ResolveSessionOwnershipAsync(userId, role, request, cancellationToken),
            ComplaintSubjectType.Enrollment => await ResolveEnrollmentOwnershipAsync(userId, role, request, cancellationToken),
            ComplaintSubjectType.Payment => await ResolvePaymentOwnershipAsync(userId, role, request, cancellationToken),
            ComplaintSubjectType.Refund => await ResolveRefundOwnershipAsync(userId, role, request, cancellationToken),
            ComplaintSubjectType.OpenSessionRequest => await ResolveOpenRequestOwnershipAsync(userId, role, request, cancellationToken),
            ComplaintSubjectType.Other => new OwnershipContext
            {
                AffectedStudentId = request.AffectedStudentId,
            },
            _ => throw new InvalidOperationException("Unsupported subject type."),
        };
    }

    private async Task<OwnershipContext> ResolveSessionOwnershipAsync(
        int userId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var scheduleId = request.CourseScheduleId!.Value;
        var schedule = await _db.CourseSchedules.AsNoTracking()
            .Include(s => s.Enrollment).ThenInclude(e => e.Participants)
            .Include(s => s.Enrollment).ThenInclude(e => e.Course)
            .Include(s => s.TeacherAvailability).ThenInclude(a => a!.TimeSlot)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        var teacherId = schedule.Enrollment.ApprovedByTeacherId > 0
            ? schedule.Enrollment.ApprovedByTeacherId
            : schedule.Enrollment.Course?.TeacherId ?? 0;

        if (role == ComplaintComplainantRole.Teacher)
        {
            var teacher = await _db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
            if (teacher == null || teacher.Id != teacherId)
                throw new InvalidOperationException("Teacher does not own this session.");
            return new OwnershipContext
            {
                CourseScheduleId = scheduleId,
                EnrollmentId = schedule.EnrollmentId,
                RespondentTeacherId = teacherId,
                AffectedStudentId = request.AffectedStudentId,
            };
        }

        var studentId = request.AffectedStudentId;
        if (studentId is null or <= 0)
        {
            var ownStudent = await _db.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            studentId = ownStudent?.Id;
        }

        if (studentId is null or <= 0)
            throw new InvalidOperationException("Affected student is required.");

        if (!schedule.Enrollment.Participants.Any(p => p.StudentId == studentId))
            throw new InvalidOperationException("Student is not a participant in this enrollment.");

        await EnsureStudentAccessAsync(userId, role, studentId.Value, cancellationToken);

        if (!ComplaintRules.CanStudentFileSessionComplaint(schedule.Status, schedule.TeacherAttendanceStatus))
            throw new InvalidOperationException("Complaint cannot be filed for this session status.");

        DateTime? startsAt = null;
        if (schedule.TeacherAvailability?.TimeSlot != null)
        {
            var slot = schedule.TeacherAvailability.TimeSlot;
            startsAt = schedule.Date.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime), DateTimeKind.Utc);
        }

        return new OwnershipContext
        {
            CourseScheduleId = scheduleId,
            EnrollmentId = schedule.EnrollmentId,
            AffectedStudentId = studentId,
            RespondentTeacherId = teacherId > 0 ? teacherId : null,
            SessionStartsAtUtc = startsAt,
            RelatedAmount = schedule.Enrollment.AmountDue,
        };
    }

    private async Task<OwnershipContext> ResolveEnrollmentOwnershipAsync(
        int userId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var enrollment = await _db.Enrollments.AsNoTracking()
            .Include(e => e.Participants)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException("Enrollment not found.");

        if (role == ComplaintComplainantRole.Teacher)
        {
            var teacher = await _db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
            var teacherId = enrollment.ApprovedByTeacherId > 0
                ? enrollment.ApprovedByTeacherId
                : enrollment.Course?.TeacherId ?? 0;
            if (teacher == null || teacher.Id != teacherId)
                throw new InvalidOperationException("Teacher does not own this enrollment.");
            return new OwnershipContext
            {
                EnrollmentId = enrollment.Id,
                RespondentTeacherId = teacherId,
            };
        }

        var studentId = request.AffectedStudentId
            ?? enrollment.Participants.FirstOrDefault()?.StudentId
            ?? throw new InvalidOperationException("Affected student is required.");

        if (!enrollment.Participants.Any(p => p.StudentId == studentId))
            throw new InvalidOperationException("Student is not on this enrollment.");

        await EnsureStudentAccessAsync(userId, role, studentId, cancellationToken);

        return new OwnershipContext
        {
            EnrollmentId = enrollment.Id,
            AffectedStudentId = studentId,
            RespondentTeacherId = enrollment.ApprovedByTeacherId > 0 ? enrollment.ApprovedByTeacherId : enrollment.Course?.TeacherId,
            RelatedAmount = enrollment.AmountDue,
        };
    }

    private async Task<OwnershipContext> ResolvePaymentOwnershipAsync(
        int userId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        if (role is ComplaintComplainantRole.Student or ComplaintComplainantRole.Guardian)
        {
            if (payment.PayerUserId != userId)
                throw new InvalidOperationException("Payment does not belong to this user.");
        }
        else if (role != ComplaintComplainantRole.Admin)
            throw new InvalidOperationException("Teachers cannot file payment complaints.");

        return new OwnershipContext
        {
            PaymentId = payment.Id,
            EnrollmentId = request.EnrollmentId,
            AffectedStudentId = request.AffectedStudentId,
            RelatedAmount = payment.TotalAmount,
        };
    }

    private async Task<OwnershipContext> ResolveRefundOwnershipAsync(
        int userId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var refund = await _db.Refunds.AsNoTracking()
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == request.RefundId, cancellationToken)
            ?? throw new InvalidOperationException("Refund not found.");

        if (role is ComplaintComplainantRole.Student or ComplaintComplainantRole.Guardian)
        {
            if (refund.Payment?.PayerUserId != userId && refund.InitiatedByUserId != userId)
                throw new InvalidOperationException("Refund does not belong to this user.");
        }

        return new OwnershipContext
        {
            RefundId = refund.Id,
            PaymentId = refund.PaymentId,
            EnrollmentId = refund.EnrollmentId,
            AffectedStudentId = request.AffectedStudentId,
            RelatedAmount = refund.Amount,
        };
    }

    private async Task<OwnershipContext> ResolveOpenRequestOwnershipAsync(
        int userId,
        ComplaintComplainantRole role,
        FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var osr = await _db.OpenSessionRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.OpenSessionRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Open session request not found.");

        if (role == ComplaintComplainantRole.Teacher)
        {
            var teacher = await _db.Teachers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken)
                ?? throw new InvalidOperationException("Teacher not found.");
            var hasOffer = await _db.OpenSessionOffers.AsNoTracking()
                .AnyAsync(o => o.SessionRequestId == osr.Id && o.TeacherId == teacher.Id, cancellationToken);
            if (!hasOffer)
                throw new InvalidOperationException("Teacher has no offer on this request.");
            return new OwnershipContext
            {
                OpenSessionRequestId = osr.Id,
                AffectedStudentId = osr.StudentId,
                RespondentTeacherId = teacher.Id,
            };
        }

        if (osr.RequestedByUserId != userId)
        {
            await EnsureStudentAccessAsync(userId, role, osr.StudentId, cancellationToken);
        }

        return new OwnershipContext
        {
            OpenSessionRequestId = osr.Id,
            AffectedStudentId = osr.StudentId,
        };
    }

    private async Task EnsureStudentAccessAsync(
        int userId,
        ComplaintComplainantRole role,
        int studentId,
        CancellationToken cancellationToken)
    {
        var student = await _db.Students.AsNoTracking()
            .Include(s => s.Guardian)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        if (student.UserId == userId)
            return;

        if (role == ComplaintComplainantRole.Guardian
            && student.Guardian?.UserId == userId)
            return;

        throw new InvalidOperationException("You do not have access to this student.");
    }

    private static bool CanView(Complaint c, int? viewerUserId, string viewerRole, int? teacherId)
    {
        if (viewerRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            || viewerRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        if (viewerUserId.HasValue && c.ComplainantUserId == viewerUserId)
            return true;

        if (teacherId.HasValue && c.RespondentTeacherId == teacherId)
            return true;

        if (viewerUserId.HasValue && c.RespondentUserId == viewerUserId)
            return true;

        return false;
    }

    private async Task<ComplaintListItemDto> MapListItemAsync(
        Complaint c,
        string viewerRole,
        CancellationToken cancellationToken)
    {
        var complainantName = await ResolveUserNameAsync(c.ComplainantUserId, cancellationToken);
        string? respondentName = null;
        if (c.RespondentTeacherId.HasValue)
        {
            var teacher = await _db.Teachers.AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == c.RespondentTeacherId, cancellationToken);
            if (teacher?.User != null)
                respondentName = $"{teacher.User.FirstName} {teacher.User.LastName}".Trim();
        }

        return new ComplaintListItemDto
        {
            ComplaintId = c.Id,
            SubjectType = c.SubjectType.ToString(),
            SubjectLabel = BuildSubjectLabel(c),
            Status = c.Status.ToString(),
            Priority = c.Priority.ToString(),
            ReasonCode = c.ReasonCode.ToString(),
            DescriptionPreview = c.Description.Length > 120 ? c.Description[..120] + "…" : c.Description,
            FiledAt = c.FiledAt,
            ComplainantName = complainantName,
            ComplainantRole = c.ComplainantRole.ToString(),
            RespondentName = respondentName,
            AssignedToUserId = c.AssignedToUserId,
            CourseScheduleId = c.CourseScheduleId,
            EnrollmentId = c.EnrollmentId,
            PaymentId = c.PaymentId,
            RefundId = c.RefundId,
            OpenSessionRequestId = c.OpenSessionRequestId,
            RequiresAction = c.Status is ComplaintStatus.Submitted
                or ComplaintStatus.InReview
                or ComplaintStatus.DecisionPending
                || (viewerRole.Equals("Teacher", StringComparison.OrdinalIgnoreCase) && c.RequiresRespondentResponse)
                || (viewerRole is "Student" or "Guardian" && c.RequiresComplainantResponse),
        };
    }

    private async Task<ComplaintDetailDto> MapDetailAsync(
        Complaint c,
        string viewerRole,
        CancellationToken cancellationToken)
    {
        var list = await MapListItemAsync(c, viewerRole, cancellationToken);
        var detail = new ComplaintDetailDto
        {
            ComplaintId = list.ComplaintId,
            SubjectType = list.SubjectType,
            SubjectLabel = list.SubjectLabel,
            Status = list.Status,
            Priority = list.Priority,
            ReasonCode = list.ReasonCode,
            DescriptionPreview = list.DescriptionPreview,
            FiledAt = list.FiledAt,
            ComplainantName = list.ComplainantName,
            ComplainantRole = list.ComplainantRole,
            RespondentName = list.RespondentName,
            AssignedToUserId = list.AssignedToUserId,
            CourseScheduleId = list.CourseScheduleId,
            EnrollmentId = list.EnrollmentId,
            PaymentId = list.PaymentId,
            RefundId = list.RefundId,
            OpenSessionRequestId = list.OpenSessionRequestId,
            RequiresAction = list.RequiresAction,
            Description = c.Description,
            ComplainantUserId = c.ComplainantUserId,
            AffectedStudentId = c.AffectedStudentId,
            RespondentTeacherId = c.RespondentTeacherId,
            RespondentUserId = c.RespondentUserId,
            RequiresRespondentResponse = c.RequiresRespondentResponse,
            RequiresComplainantResponse = c.RequiresComplainantResponse,
            RespondentResponse = c.RespondentResponse,
            RespondentRespondedAt = c.RespondentRespondedAt,
            ComplainantResponse = c.ComplainantResponse,
            ComplainantRespondedAt = c.ComplainantRespondedAt,
            ResolutionCode = c.ResolutionCode?.ToString(),
            ResolutionNotes = c.ResolutionNotes,
            ResolvedAt = c.ResolvedAt,
            LinkedRefundId = c.LinkedRefundId,
            ReplacementScheduleId = c.ReplacementScheduleId,
            AvailableActions = BuildAvailableActions(c, viewerRole),
            Attachments = c.Attachments
                .OrderBy(a => a.UploadedAt)
                .Select(a => new ComplaintAttachmentDto
                {
                    AttachmentId = a.Id,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    ContentType = a.ContentType,
                })
                .ToList(),
            Timeline = c.Timeline
                .OrderBy(t => t.OccurredAt)
                .ThenBy(t => t.Id)
                .Select(t => new ComplaintTimelineDto
                {
                    EntryId = t.Id,
                    EventType = t.EventType.ToString(),
                    FromStatus = t.FromStatus?.ToString(),
                    ToStatus = t.ToStatus?.ToString(),
                    ActorUserId = t.ActorUserId,
                    ActorRole = t.ActorRole,
                    Notes = t.Notes,
                    OccurredAt = t.OccurredAt,
                })
                .ToList(),
        };
        return detail;
    }

    private static List<string> BuildAvailableActions(Complaint c, string viewerRole)
    {
        var actions = new List<string>();
        var isAdmin = viewerRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                      || viewerRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

        if (isAdmin && ComplaintRules.IsOpen(c.Status))
        {
            actions.Add("Assign");
            actions.Add("SetPriority");
            if (c.Status is ComplaintStatus.Submitted or ComplaintStatus.InReview
                or ComplaintStatus.AwaitingComplainant or ComplaintStatus.AwaitingRespondent)
            {
                actions.Add("RequestInfoComplainant");
                actions.Add("RequestInfoRespondent");
            }

            if (ComplaintRules.CanTransition(c.Status, ComplaintStatus.InReview)
                && c.Status == ComplaintStatus.Submitted)
                actions.Add("AdvanceInReview");

            if (ComplaintRules.CanTransition(c.Status, ComplaintStatus.DecisionPending))
                actions.Add("AdvanceDecisionPending");

            if (c.Status is ComplaintStatus.DecisionPending or ComplaintStatus.InReview)
            {
                actions.Add("Resolve");
                actions.Add("Reject");
                actions.Add("PreviewResolve");
            }

            actions.Add("Cancel");
        }

        if ((viewerRole is "Student" or "Guardian")
            && (c.Status == ComplaintStatus.AwaitingComplainant || c.RequiresComplainantResponse))
            actions.Add("RespondAsComplainant");

        if (viewerRole.Equals("Teacher", StringComparison.OrdinalIgnoreCase)
            && (c.Status == ComplaintStatus.AwaitingRespondent || c.RequiresRespondentResponse))
            actions.Add("RespondAsRespondent");

        return actions;
    }

    private static string BuildSubjectLabel(Complaint c) =>
        c.SubjectType switch
        {
            ComplaintSubjectType.Session => c.CourseScheduleId is int sid
                ? $"Session #{sid}" + (c.EnrollmentId is int eid ? $" · EN-{eid}" : "")
                : "Session",
            ComplaintSubjectType.Enrollment => c.EnrollmentId is int eid ? $"EN-{eid}" : "Enrollment",
            ComplaintSubjectType.Payment => c.PaymentId is int pid ? $"PAY-{pid}" : "Payment",
            ComplaintSubjectType.Refund => c.RefundId is int rid ? $"REF-{rid}" : "Refund",
            ComplaintSubjectType.OpenSessionRequest => c.OpenSessionRequestId is int oid
                ? $"OSR-{oid}"
                : "Open session request",
            _ => "Other",
        };

    private async Task<string?> ResolveUserNameAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            return null;
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.UserName : name;
    }

    private async Task<Complaint> GetTrackedAsync(int id, CancellationToken cancellationToken) =>
        await _complaints.GetByIdTrackedAsync(id, cancellationToken)
        ?? throw new InvalidOperationException("Complaint not found.");

    private async Task AddTimelineAsync(
        int complaintId,
        ComplaintTimelineEventType eventType,
        ComplaintStatus? from,
        ComplaintStatus? to,
        int actorUserId,
        string actorRole,
        string? notes,
        CancellationToken cancellationToken)
    {
        await _complaints.AddTimelineAsync(new ComplaintTimelineEntry
        {
            ComplaintId = complaintId,
            EventType = eventType,
            FromStatus = from,
            ToStatus = to,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Notes = notes,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
    }

    private async Task SaveAttachmentAsync(
        IFormFile file,
        int complaintId,
        int userId,
        CancellationToken cancellationToken)
    {
        if (!await _fileStorage.ValidateFileAsync(file, AllowedAttachmentExtensions, MaxAttachmentSizeBytes))
            throw new InvalidOperationException(
                "Invalid attachment — allowed: PDF/PNG/JPG/WEBP, max 25 MB.");

        var attachment = new ComplaintAttachment
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

        var storageKey = $"complaints/{complaintId}/{attachment.Id}{ext}";
        var ossPublicBase = _storagePublicUrls.GetLearningPublicBaseUrl();
        if (string.IsNullOrWhiteSpace(ossPublicBase))
            throw new InvalidOperationException(
                "Learning public base URL is not configured; cannot upload complaint attachments.");

        attachment.FileUrl = $"{ossPublicBase.TrimEnd('/')}/{storageKey}";
        await _complaints.SaveChangesAsync(cancellationToken);

        try
        {
            // Reuse session-complaint upload queue shape if available; otherwise store URL only.
            await _fileStorage.QueueSessionComplaintAttachmentUploadAsync(
                file, complaintId, attachment.Id, storageKey);
        }
        catch
        {
            await _complaints.RemoveAttachmentAsync(attachment.Id, cancellationToken);
            throw;
        }
    }

    private sealed class OwnershipContext
    {
        public int? CourseScheduleId { get; init; }
        public int? EnrollmentId { get; init; }
        public int? PaymentId { get; init; }
        public int? RefundId { get; init; }
        public int? OpenSessionRequestId { get; init; }
        public int? AffectedStudentId { get; init; }
        public int? RespondentTeacherId { get; init; }
        public int? RespondentUserId { get; init; }
        public decimal? RelatedAmount { get; init; }
        public DateTime? SessionStartsAtUtc { get; init; }
    }
}
