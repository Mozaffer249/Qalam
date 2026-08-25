using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class OpenSessionRequestRepository : GenericRepositoryAsync<OpenSessionRequest>, IOpenSessionRequestRepository
{
    private readonly ApplicationDBContext _context;

    public OpenSessionRequestRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<int?> GetSubjectIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => (int?)r.SubjectId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<int> ContentTypeIds, List<int> LevelIds)> GetSessionQuranRequirementIdsAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.OpenSessionRequestSessions
            .AsNoTracking()
            .Where(s => s.SessionRequestId == requestId)
            .Select(s => new { s.QuranContentTypeId, s.QuranLevelId })
            .ToListAsync(cancellationToken);

        var contentTypeIds = rows
            .Where(r => r.QuranContentTypeId.HasValue)
            .Select(r => r.QuranContentTypeId!.Value)
            .Distinct()
            .ToList();
        var levelIds = rows
            .Where(r => r.QuranLevelId.HasValue)
            .Select(r => r.QuranLevelId!.Value)
            .Distinct()
            .ToList();

        return (contentTypeIds, levelIds);
    }

    public async Task<List<int>> GetOpenBroadcastRequestIdsBySubjectIdsAsync(
        IReadOnlyCollection<int> subjectIds,
        CancellationToken cancellationToken = default)
    {
        if (subjectIds.Count == 0) return new List<int>();

        var now = DateTime.UtcNow;
        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => subjectIds.Contains(r.SubjectId)
                        && r.TargetedTeacherId == null
                        && (r.Status == OpenSessionRequestStatus.Active
                            || r.Status == OpenSessionRequestStatus.ReceivingOffers)
                        && (r.ExpiresAt == null || r.ExpiresAt > now))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeacherAvailableRequestDetailDto?> GetTeacherDetailDtoAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await (
            from r in _context.OpenSessionRequests.AsNoTracking()
            where r.Id == requestId
            select new TeacherAvailableRequestDetailDto
            {
                Id = r.Id,
                Status = r.Status,
                IsTargeted = r.TargetedTeacherId != null,
                Content = new RequestContentDto
                {
                    DomainId = r.DomainId,
                    DomainCode = r.Domain != null ? r.Domain.Code : null,
                    DomainNameEn = r.Domain != null ? r.Domain.NameEn : null,
                    DomainNameAr = r.Domain != null ? r.Domain.NameAr : null,
                    CurriculumId = r.CurriculumId,
                    CurriculumNameEn = r.Curriculum != null ? r.Curriculum.NameEn : null,
                    CurriculumNameAr = r.Curriculum != null ? r.Curriculum.NameAr : null,
                    LevelId = r.LevelId,
                    LevelNameEn = r.Level != null ? r.Level.NameEn : null,
                    LevelNameAr = r.Level != null ? r.Level.NameAr : null,
                    GradeId = r.GradeId,
                    GradeNameEn = r.Grade != null ? r.Grade.NameEn : null,
                    GradeNameAr = r.Grade != null ? r.Grade.NameAr : null,
                    SubjectId = r.SubjectId,
                    SubjectNameEn = r.Subject != null ? r.Subject.NameEn : null,
                    SubjectNameAr = r.Subject != null ? r.Subject.NameAr : null,
                },
                GeneralSettings = new RequestGeneralSettingsDto
                {
                    SessionsCount = r.TotalSessionsCount,
                    DefaultDurationMinutes = r.Sessions.Select(s => (int?)s.DurationMinutes).FirstOrDefault(),
                    TeachingModeId = r.TeachingModeId,
                    TeachingModeNameEn = r.TeachingMode != null ? r.TeachingMode.NameEn : null,
                    TeachingModeNameAr = r.TeachingMode != null ? r.TeachingMode.NameAr : null,
                    GroupType = r.GroupType,
                    StudentNotes = r.StudentNotes,
                },
                Sessions = r.Sessions
                    .OrderBy(s => s.SequenceNumber)
                    .Select(s => new TeacherViewSessionDto
                    {
                        Id = s.Id,
                        SequenceNumber = s.SequenceNumber,
                        PreferredDate = s.PreferredDate,
                        TimeSlotId = s.TimeSlotId,
                        TimeSlotLabelEn = s.TimeSlot != null ? s.TimeSlot.LabelEn : null,
                        TimeSlotLabelAr = s.TimeSlot != null ? s.TimeSlot.LabelAr : null,
                        StartTime = s.TimeSlot != null ? s.TimeSlot.StartTime : null,
                        EndTime = s.TimeSlot != null ? s.TimeSlot.EndTime : null,
                        DurationMinutes = s.DurationMinutes,
                        Notes = s.Notes,
                        Units = s.Units.Select(u => new TeacherViewSessionUnitDto
                        {
                            Id = u.Id,
                            ContentUnitId = u.ContentUnitId,
                            ContentUnitNameEn = u.ContentUnit != null ? u.ContentUnit.NameEn : null,
                            ContentUnitNameAr = u.ContentUnit != null ? u.ContentUnit.NameAr : null,
                            LessonId = u.LessonId,
                            LessonNameEn = u.Lesson != null ? u.Lesson.NameEn : null,
                            LessonNameAr = u.Lesson != null ? u.Lesson.NameAr : null,
                            CustomUnitLabel = u.CustomUnitLabel,
                            IncludesAllLessons = u.IncludesAllLessons,
                        }).ToList()
                    }).ToList(),
                Attachments = r.Attachments
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new TeacherViewAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        PublicUrl = a.PublicUrl,
                        CreatedAt = a.CreatedAt,
                    }).ToList(),
                Student = new RequestStudentSummaryDto
                {
                    Id = r.StudentId,
                    DisplayName = r.RequestedByUser != null
                        ? ((r.RequestedByUser.FirstName ?? "") + " " + (r.RequestedByUser.LastName ?? "")).Trim()
                        : null,
                },
                CurrentOffersCount = r.Offers.Count(o => o.Status != OpenSessionOfferStatus.Withdrawn),
                MyOfferStatus = null,
                MyOfferId = null,
                ExpiresAt = r.ExpiresAt ?? DateTime.MinValue,
                PublishedAt = r.PublishedAt ?? DateTime.MinValue,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<RequestSessionScheduleSlot>> GetSessionScheduleSlotsAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequestSessions
            .AsNoTracking()
            .Where(s => s.SessionRequestId == requestId)
            .OrderBy(s => s.SequenceNumber)
            .Select(s => new RequestSessionScheduleSlot(
                s.Id,
                s.SequenceNumber,
                s.PreferredDate,
                s.TimeSlotId,
                s.DurationMinutes,
                s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.StartTime : null,
                s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.EndTime : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveOffersAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionOffers
            .AsNoTracking()
            .CountAsync(o => o.SessionRequestId == requestId
                             && o.Status != OpenSessionOfferStatus.Withdrawn, cancellationToken);
    }

    public async Task<RequestStatusSummary?> GetStatusSummaryAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await (
            from r in _context.OpenSessionRequests.AsNoTracking()
            where r.Id == requestId
            join st in _context.Students.AsNoTracking() on r.RequestedByUserId equals st.UserId into studentJoin
            from student in studentJoin.DefaultIfEmpty()
            select new RequestStatusSummary(
                r.Id,
                student != null ? student.Id : 0,
                r.RequestedByUserId,
                r.CreatedByGuardianId,
                r.Status,
                r.TargetedTeacherId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OpenSessionRequest?> GetForPublishAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequests
            .Include(r => r.Sessions).ThenInclude(s => s.Units)
            .Include(r => r.Sessions).ThenInclude(s => s.TimeSlot)
            .Include(r => r.Invitations)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<OpenSessionRequest?> GetStudentDetailAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Include(r => r.Student).ThenInclude(s => s!.User)
            .Include(r => r.CreatedByGuardian).ThenInclude(g => g!.User)
            .Include(r => r.Domain)
            .Include(r => r.Curriculum)
            .Include(r => r.Level)
            .Include(r => r.Grade)
            .Include(r => r.Term)
            .Include(r => r.University)
            .Include(r => r.College)
            .Include(r => r.Department)
            .Include(r => r.AcademicProgram)
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.Sessions).ThenInclude(s => s.QuranContentType)
            .Include(r => r.Sessions).ThenInclude(s => s.QuranLevel)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .Include(r => r.Offers)
            .Include(r => r.PricingSnapshot)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task<List<StudentInvitationListItemDto>> GetPendingInvitationListItemsAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken = default)
    {
        if (studentIds.Count == 0)
            return new List<StudentInvitationListItemDto>();

        return await _context.OpenSessionRequestInvitations
            .AsNoTracking()
            .Where(i => studentIds.Contains(i.InvitedStudentId)
                        && i.Status == OpenSessionRequestInvitationStatus.Pending
                        && i.OpenSessionRequest.Status == OpenSessionRequestStatus.PendingInvitations)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new StudentInvitationListItemDto
            {
                Source = "OpenSessionRequest",
                InvitationId = i.Id,
                EnrollmentRequestId = null,
                OpenSessionRequestId = i.SessionRequestId,
                CourseId = null,
                CourseTitle = null,
                CourseImageUrl = null,
                TeacherDisplayName = null,
                TitleEn = i.OpenSessionRequest.Subject != null
                    ? i.OpenSessionRequest.Subject.NameEn
                    : null,
                TitleAr = i.OpenSessionRequest.Subject != null
                    ? i.OpenSessionRequest.Subject.NameAr
                    : null,
                InvitedStudentId = i.InvitedStudentId,
                InvitedStudentName = i.InvitedStudent != null && i.InvitedStudent.User != null
                    ? (i.InvitedStudent.User.FirstName + " " + i.InvitedStudent.User.LastName).Trim()
                    : null,
                RequestedByUserName = i.OpenSessionRequest.RequestedByUser != null
                    ? (i.OpenSessionRequest.RequestedByUser.FirstName + " "
                       + i.OpenSessionRequest.RequestedByUser.LastName).Trim()
                    : null,
                CreatedAt = i.CreatedAt,
                ConfirmationStatus = null,
                IsOwner = false,
                InvitedStudentCount = i.OpenSessionRequest.Invitations.Count(),
                IsGroup = i.OpenSessionRequest.Invitations.Count() > 1
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StudentInvitationListItemDto>> GetReceivedInvitationListItemsAsync(
        IReadOnlyCollection<int> studentIds,
        InvitationInboxScope scope,
        CancellationToken cancellationToken = default)
    {
        if (studentIds.Count == 0)
            return new List<StudentInvitationListItemDto>();

        var query = _context.OpenSessionRequestInvitations
            .AsNoTracking()
            .Where(i => studentIds.Contains(i.InvitedStudentId));

        if (scope == InvitationInboxScope.Active)
        {
            query = query.Where(i =>
                i.Status == OpenSessionRequestInvitationStatus.Pending
                && i.OpenSessionRequest.Status != OpenSessionRequestStatus.Cancelled
                && i.OpenSessionRequest.Status != OpenSessionRequestStatus.Expired
                && i.OpenSessionRequest.Status != OpenSessionRequestStatus.Rejected);
        }
        else
        {
            query = query.Where(i =>
                i.Status != OpenSessionRequestInvitationStatus.Pending
                || i.OpenSessionRequest.Status == OpenSessionRequestStatus.Cancelled
                || i.OpenSessionRequest.Status == OpenSessionRequestStatus.Expired
                || i.OpenSessionRequest.Status == OpenSessionRequestStatus.Rejected);
        }

        var rows = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.SessionRequestId,
                i.InvitedStudentId,
                i.Status,
                i.CreatedAt,
                RequestStatus = i.OpenSessionRequest.Status,
                TitleEn = i.OpenSessionRequest.Subject != null
                    ? i.OpenSessionRequest.Subject.NameEn
                    : null,
                TitleAr = i.OpenSessionRequest.Subject != null
                    ? i.OpenSessionRequest.Subject.NameAr
                    : null,
                InvitedStudentName = i.InvitedStudent != null && i.InvitedStudent.User != null
                    ? (i.InvitedStudent.User.FirstName + " " + i.InvitedStudent.User.LastName).Trim()
                    : null,
                RequestedByUserName = i.OpenSessionRequest.RequestedByUser != null
                    ? (i.OpenSessionRequest.RequestedByUser.FirstName + " "
                       + i.OpenSessionRequest.RequestedByUser.LastName).Trim()
                    : null,
                InvitedStudentCount = i.OpenSessionRequest.Invitations.Count()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(i => new StudentInvitationListItemDto
        {
            Source = "OpenSessionRequest",
            InvitationId = i.Id,
            EnrollmentRequestId = null,
            OpenSessionRequestId = i.SessionRequestId,
            CourseId = null,
            CourseTitle = null,
            CourseImageUrl = null,
            TeacherDisplayName = null,
            TitleEn = i.TitleEn,
            TitleAr = i.TitleAr,
            InvitedStudentId = i.InvitedStudentId,
            InvitedStudentName = i.InvitedStudentName,
            RequestedByUserName = i.RequestedByUserName,
            CreatedAt = i.CreatedAt,
            ConfirmationStatus = MapOsrInviteToConfirmationStatus(i.Status),
            IsOwner = false,
            ParentStatus = i.RequestStatus.ToString(),
            InvitedStudentCount = i.InvitedStudentCount,
            IsGroup = i.InvitedStudentCount > 1
        }).ToList();
    }

    private static GroupMemberConfirmationStatus MapOsrInviteToConfirmationStatus(
        OpenSessionRequestInvitationStatus status)
        => status switch
        {
            OpenSessionRequestInvitationStatus.Pending => GroupMemberConfirmationStatus.Pending,
            OpenSessionRequestInvitationStatus.Accepted => GroupMemberConfirmationStatus.Confirmed,
            OpenSessionRequestInvitationStatus.Rejected => GroupMemberConfirmationStatus.Rejected,
            OpenSessionRequestInvitationStatus.Expired => GroupMemberConfirmationStatus.Cancelled,
            _ => GroupMemberConfirmationStatus.Pending
        };

    public async Task<List<StudentInvitationListItemDto>> GetSentInvitationListItemsAsync(
        int userId,
        int? guardianId,
        InvitationInboxScope scope,
        CancellationToken cancellationToken = default)
    {
        var invitations = _context.OpenSessionRequestInvitations.AsNoTracking();
        if (guardianId.HasValue)
        {
            invitations = invitations.Where(i =>
                i.OpenSessionRequest.RequestedByUserId == userId
                || i.OpenSessionRequest.CreatedByGuardianId == guardianId);
        }
        else
        {
            invitations = invitations.Where(i => i.OpenSessionRequest.RequestedByUserId == userId);
        }

        var members = await invitations
            .Select(i => new
            {
                i.Id,
                i.InvitedStudentId,
                i.Status,
                i.CreatedAt,
                i.SessionRequestId,
                RequestCreatedAt = i.OpenSessionRequest.CreatedAt,
                RequestStatus = i.OpenSessionRequest.Status,
                TitleEn = i.OpenSessionRequest.Subject != null
                    ? i.OpenSessionRequest.Subject.NameEn
                    : null,
                TitleAr = i.OpenSessionRequest.Subject != null
                    ? i.OpenSessionRequest.Subject.NameAr
                    : null,
                InvitedStudentName = i.InvitedStudent != null && i.InvitedStudent.User != null
                    ? (i.InvitedStudent.User.FirstName + " " + i.InvitedStudent.User.LastName).Trim()
                    : null,
                RequestedByUserName = i.OpenSessionRequest.RequestedByUser != null
                    ? (i.OpenSessionRequest.RequestedByUser.FirstName + " "
                       + i.OpenSessionRequest.RequestedByUser.LastName).Trim()
                    : null
            })
            .ToListAsync(cancellationToken);

        return members
            .GroupBy(m => m.SessionRequestId)
            .Select(g =>
            {
                var invite = g
                    .OrderBy(m => m.Status == OpenSessionRequestInvitationStatus.Pending ? 0 : 1)
                    .ThenBy(m => m.CreatedAt)
                    .First();
                var request = g.First();
                var hasPending = g.Any(m => m.Status == OpenSessionRequestInvitationStatus.Pending);
                var parent = request.RequestStatus;
                var parentTerminal = parent is OpenSessionRequestStatus.Cancelled
                    or OpenSessionRequestStatus.Expired
                    or OpenSessionRequestStatus.Rejected;
                var waitingParent = parent is OpenSessionRequestStatus.Draft
                    or OpenSessionRequestStatus.PendingInvitations;
                var isActive = !parentTerminal && (waitingParent || hasPending);
                var invitedCount = g.Count();
                return new { invite, request, isActive, invitedCount };
            })
            .Where(x => scope == InvitationInboxScope.Active ? x.isActive : !x.isActive)
            .Select(x => new StudentInvitationListItemDto
            {
                Source = "OpenSessionRequest",
                InvitationId = x.invite.Id,
                EnrollmentRequestId = null,
                OpenSessionRequestId = x.request.SessionRequestId,
                CourseId = null,
                CourseTitle = null,
                CourseImageUrl = null,
                TeacherDisplayName = null,
                TitleEn = x.request.TitleEn,
                TitleAr = x.request.TitleAr,
                InvitedStudentId = x.invite.InvitedStudentId,
                InvitedStudentName = x.invite.InvitedStudentName,
                RequestedByUserName = x.request.RequestedByUserName,
                CreatedAt = x.request.RequestCreatedAt,
                ConfirmationStatus = null,
                IsOwner = true,
                ParentStatus = x.request.RequestStatus.ToString(),
                InvitedStudentCount = x.invitedCount,
                IsGroup = x.invitedCount > 1
            })
            .ToList();
    }

    public async Task<bool> UpdateStatusAsync(int requestId, OpenSessionRequestStatus newStatus, CancellationToken cancellationToken = default)
    {
        var entity = await _context.OpenSessionRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null) return false;

        entity.Status = newStatus;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DateTime?> GetExpiresAtAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var row = await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);
        return row?.ExpiresAt;
    }

    public async Task<List<ExpiredRequestResult>> ExpirePastCutoffRequestsAsync(
        DateTime nowUtc,
        OpenSessionRequestSettings settings,
        CancellationToken cancellationToken = default)
    {
        var expirables = OpenSessionRequestStatusSets.Expirable;
        var platformToday = PlatformTime.ToPlatformDate(nowUtc);
        var nearDate = platformToday.AddDays(1);

        // SQL-friendly candidate filter; precise cutoff applied in memory.
        var candidates = await _context.OpenSessionRequests
            .Include(r => r.Offers)
            .Include(r => r.Invitations)
            .Include(r => r.Sessions).ThenInclude(s => s.TimeSlot)
            .Where(r => expirables.Contains(r.Status)
                        && (
                            (r.ExpiresAt != null && r.ExpiresAt < nowUtc)
                            || r.Sessions.Any(s => s.PreferredDate != null && s.PreferredDate <= nearDate)
                        ))
            .ToListAsync(cancellationToken);

        var results = new List<ExpiredRequestResult>();
        if (candidates.Count == 0)
            return results;

        foreach (var request in candidates)
        {
            var isTargeted = request.TargetedTeacherId != null;
            var firstStart = OpenSessionRequestExpiry.FirstSessionStartUtc(
                request.Sessions.Select(s => (
                    s.PreferredDate,
                    s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.StartTime : null)));

            var effective = OpenSessionRequestExpiry.EffectiveExpiryUtc(
                request.ExpiresAt, firstStart, settings, isTargeted);

            if (effective > nowUtc)
                continue;

            request.Status = OpenSessionRequestStatus.Expired;
            request.UpdatedAt = nowUtc;

            foreach (var offer in request.Offers.Where(o => o.Status == OpenSessionOfferStatus.Pending))
            {
                offer.Status = OpenSessionOfferStatus.Withdrawn;
                offer.WithdrawnAt = nowUtc;
                offer.UpdatedAt = nowUtc;
            }

            foreach (var invite in request.Invitations.Where(i =>
                         i.Status == OpenSessionRequestInvitationStatus.Pending))
            {
                invite.Status = OpenSessionRequestInvitationStatus.Expired;
                invite.RespondedAt = nowUtc;
            }

            results.Add(new ExpiredRequestResult(
                request.Id,
                request.RequestedByUserId,
                effective,
                OpenSessionRequestExpiry.IsWithinNotificationGrace(effective, nowUtc, settings)));
        }

        if (results.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return results;
    }

    public async Task<List<InviteExpiryFinalizeResult>> ExpireStalePendingInvitationsAsync(
        DateTime nowUtc,
        int inviteResponseDeadlineHours,
        CancellationToken cancellationToken = default)
    {
        var hours = Math.Max(1, inviteResponseDeadlineHours);
        var cutoff = nowUtc.AddHours(-hours);

        var candidates = await _context.OpenSessionRequests
            .Include(r => r.Invitations)
            .Where(r => r.Status == OpenSessionRequestStatus.PendingInvitations
                        && r.Invitations.Any(i =>
                            i.Status == OpenSessionRequestInvitationStatus.Pending
                            && i.CreatedAt < cutoff))
            .ToListAsync(cancellationToken);

        var results = new List<InviteExpiryFinalizeResult>();
        if (candidates.Count == 0)
            return results;

        foreach (var request in candidates)
        {
            foreach (var invite in request.Invitations.Where(i =>
                         i.Status == OpenSessionRequestInvitationStatus.Pending
                         && i.CreatedAt < cutoff))
            {
                invite.Status = OpenSessionRequestInvitationStatus.Expired;
                invite.RespondedAt = nowUtc;
            }

            var stillPending = request.Invitations.Any(i =>
                i.Status == OpenSessionRequestInvitationStatus.Pending);
            if (stillPending)
                continue;

            var anyAccepted = request.Invitations.Any(i =>
                i.Status == OpenSessionRequestInvitationStatus.Accepted);

            if (anyAccepted)
            {
                request.Status = OpenSessionRequestStatus.Active;
                request.UpdatedAt = nowUtc;
                results.Add(new InviteExpiryFinalizeResult(
                    request.Id, request.RequestedByUserId, request.TargetedTeacherId, BecameActive: true));
            }
            else
            {
                request.Status = OpenSessionRequestStatus.Cancelled;
                request.CancelledAt = nowUtc;
                request.CancellationReason = "انتهت مهلة الرد على الدعوات";
                request.UpdatedAt = nowUtc;
                results.Add(new InviteExpiryFinalizeResult(
                    request.Id, request.RequestedByUserId, request.TargetedTeacherId, BecameActive: false));
            }
        }

        if (results.Count > 0 || candidates.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return results;
    }

    public async Task<List<int>> DemoteReceivingOffersWithoutLiveOffersAsync(
        CancellationToken cancellationToken = default)
    {
        var ids = await _context.OpenSessionRequests
            .Where(r => r.Status == OpenSessionRequestStatus.ReceivingOffers
                        && !r.Offers.Any(o => o.Status == OpenSessionOfferStatus.Pending))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
            return ids;

        var now = DateTime.UtcNow;
        var rows = await _context.OpenSessionRequests
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.Status = OpenSessionRequestStatus.Active;
            row.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ids;
    }

    public async Task<List<SettledPaymentPendingResult>> SettleAbandonedPaymentPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await (
            from r in _context.OpenSessionRequests
            where r.Status == OpenSessionRequestStatus.PaymentPending
            join e in _context.Enrollments on r.Id equals e.SessionRequestId
            where e.EnrollmentStatus == EnrollmentStatus.Cancelled
            select new { Request = r, e.CancelledAt }
        ).ToListAsync(cancellationToken);

        var results = new List<SettledPaymentPendingResult>();
        if (rows.Count == 0)
            return results;

        // Default settings for grace — caller can also filter; use CancelledAt age.
        // Notification decision is made here with a conservative 6h when settings aren't passed.
        // Lifecycle service re-evaluates with full settings before emailing.
        foreach (var row in rows)
        {
            row.Request.Status = OpenSessionRequestStatus.Expired;
            row.Request.UpdatedAt = now;
            results.Add(new SettledPaymentPendingResult(
                row.Request.Id,
                row.Request.RequestedByUserId,
                row.CancelledAt,
                Notify: true));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return results;
    }

    public async Task<List<ExpiryNudgeCandidate>> GetExpiryNudgeCandidatesAsync(
        DateTime nowUtc,
        int stageIndex,
        int hoursBeforeExpiry,
        CancellationToken cancellationToken = default)
    {
        var stageByte = (byte)stageIndex;
        var windowEnd = nowUtc.AddHours(hoursBeforeExpiry);
        var expirables = OpenSessionRequestStatusSets.Expirable;

        return await _context.OpenSessionRequests
            .AsNoTracking()
            .Where(r => expirables.Contains(r.Status)
                        && r.ExpiresAt != null
                        && r.ExpiresAt > nowUtc
                        && r.ExpiresAt <= windowEnd
                        && r.ExpiryNudgeStage <= stageByte)
            .Select(r => new ExpiryNudgeCandidate(
                r.Id,
                r.RequestedByUserId,
                r.TargetedTeacherId,
                r.ExpiresAt!.Value,
                r.ExpiryNudgeStage))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkExpiryNudgeStageAsync(int requestId, byte stage, CancellationToken cancellationToken = default)
    {
        var entity = await _context.OpenSessionRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (entity == null) return;
        if (entity.ExpiryNudgeStage >= stage) return;
        entity.ExpiryNudgeStage = stage;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
