using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.AppMetaData;
using Qalam.Data.Commons;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class StudentInvitationInboxService : IStudentInvitationInboxService
{
    private static readonly OpenSessionRequestStatus[] OsrCancellableStatuses =
    [
        OpenSessionRequestStatus.Draft,
        OpenSessionRequestStatus.PendingInvitations,
        OpenSessionRequestStatus.Active,
        OpenSessionRequestStatus.ReceivingOffers,
    ];

    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseEnrollmentRequestRepository _enrollmentRequestRepository;
    private readonly IOpenSessionRequestRepository _openSessionRequestRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly EnrollmentSettings _enrollmentSettings;

    public StudentInvitationInboxService(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ICourseEnrollmentRequestRepository enrollmentRequestRepository,
        IOpenSessionRequestRepository openSessionRequestRepository,
        IEnrollmentRepository enrollmentRepository,
        IMediaUrlResolver mediaUrlResolver,
        IOptions<EnrollmentSettings> enrollmentSettings)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _enrollmentRequestRepository = enrollmentRequestRepository;
        _openSessionRequestRepository = openSessionRequestRepository;
        _enrollmentRepository = enrollmentRepository;
        _mediaUrlResolver = mediaUrlResolver;
        _enrollmentSettings = enrollmentSettings.Value;
    }

    public async Task<StudentInvitationListResultDto> GetMyInvitationsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        InvitationInboxScope scope,
        CancellationToken cancellationToken = default)
    {
        var visibleStudentIds = await ResolveVisibleStudentIdsAsync(userId, cancellationToken);
        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        var deadlineHours = DeadlineHours();

        var inviteeS1 = visibleStudentIds.Count == 0
            ? new List<StudentInvitationListItemDto>()
            : await _enrollmentRequestRepository.GetReceivedInvitationListItemsAsync(
                visibleStudentIds, scope, cancellationToken);
        ApplyInvitationListComputedFields(
            inviteeS1, StudentInvitationDetailDto.SourceEnrollmentRequest, deadlineHours);

        var inviteeS2 = visibleStudentIds.Count == 0
            ? new List<StudentInvitationListItemDto>()
            : await _openSessionRequestRepository.GetReceivedInvitationListItemsAsync(
                visibleStudentIds, scope, cancellationToken);
        ApplyInvitationListComputedFields(
            inviteeS2, StudentInvitationDetailDto.SourceOpenSessionRequest, deadlineHours);

        var ownerS1 = await _enrollmentRequestRepository.GetSentInvitationListItemsAsync(
            userId, scope, cancellationToken);
        ApplyInvitationListComputedFields(
            ownerS1, StudentInvitationDetailDto.SourceEnrollmentRequest, deadlineHours);

        var ownerS2 = await _openSessionRequestRepository.GetSentInvitationListItemsAsync(
            userId, guardian?.Id, scope, cancellationToken);
        ApplyInvitationListComputedFields(
            ownerS2, StudentInvitationDetailDto.SourceOpenSessionRequest, deadlineHours);

        var inviteeEnrollmentIds = inviteeS1
            .Where(x => x.EnrollmentRequestId.HasValue)
            .Select(x => x.EnrollmentRequestId!.Value)
            .ToHashSet();
        var inviteeOsrIds = inviteeS2
            .Where(x => x.OpenSessionRequestId.HasValue)
            .Select(x => x.OpenSessionRequestId!.Value)
            .ToHashSet();

        var ownerS1Deduped = ownerS1.Where(x =>
            !x.EnrollmentRequestId.HasValue
            || !inviteeEnrollmentIds.Contains(x.EnrollmentRequestId.Value));
        var ownerS2Deduped = ownerS2.Where(x =>
            !x.OpenSessionRequestId.HasValue
            || !inviteeOsrIds.Contains(x.OpenSessionRequestId.Value));

        var merged = inviteeS1
            .Concat(inviteeS2)
            .Concat(ownerS1Deduped)
            .Concat(ownerS2Deduped)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var totalCount = merged.Count;
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);
        var items = merged
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return new StudentInvitationListResultDto
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<StudentInvitationDetailDto?> GetInvitationDetailAsync(
        int userId,
        string invitationKey,
        CancellationToken cancellationToken = default)
    {
        if (!StudentInvitationDetailDto.TryParseInvitationKey(invitationKey, out var source, out var invitationId))
            return null;

        var visibleStudentIds = await ResolveVisibleStudentIdsAsync(userId, cancellationToken);
        var visibleSet = visibleStudentIds.ToHashSet();

        if (source == StudentInvitationDetailDto.SourceEnrollmentRequest)
            return await GetEnrollmentRequestInvitationDetailAsync(
                userId, invitationId, visibleSet, cancellationToken);

        return await GetOpenSessionInvitationDetailAsync(
            userId, invitationId, visibleSet, cancellationToken);
    }

    private async Task<StudentInvitationDetailDto?> GetEnrollmentRequestInvitationDetailAsync(
        int userId,
        int invitationId,
        HashSet<int> visibleStudentIds,
        CancellationToken cancellationToken)
    {
        var parent = await _enrollmentRequestRepository.GetTableNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.Course).ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course).ThenInclude(c => c.Teacher).ThenInclude(t => t.User)
            .Include(r => r.Course).ThenInclude(c => c.Sessions)
            .Include(r => r.GroupMembers).ThenInclude(gm => gm.Student).ThenInclude(s => s.User)
            .Include(r => r.SelectedSessionSlots).ThenInclude(ss => ss.TeacherAvailability).ThenInclude(ta => ta.TimeSlot)
            .Include(r => r.SelectedSessionSlots).ThenInclude(ss => ss.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.SelectedSessionSlots).ThenInclude(ss => ss.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.ProposedSessions).ThenInclude(ps => ps.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.ProposedSessions).ThenInclude(ps => ps.Units).ThenInclude(u => u.Lesson)
            .FirstOrDefaultAsync(
                r => r.GroupMembers.Any(gm =>
                    gm.Id == invitationId && gm.MemberType == GroupMemberType.Invited),
                cancellationToken);

        if (parent == null)
            return null;

        var opened = parent.GroupMembers.FirstOrDefault(gm =>
            gm.Id == invitationId && gm.MemberType == GroupMemberType.Invited);
        if (opened == null)
            return null;

        var isOwner = parent.RequestedByUserId == userId;
        var memberStudentIds = parent.GroupMembers.Select(gm => gm.StudentId).ToHashSet();
        var viewerOnRequest = memberStudentIds.Where(visibleStudentIds.Contains).ToList();
        var invitedStudentIds = parent.GroupMembers
            .Where(gm => gm.MemberType == GroupMemberType.Invited)
            .Select(gm => gm.StudentId)
            .ToHashSet();
        var viewerInvitedOnRequest = invitedStudentIds.Where(visibleStudentIds.Contains).ToList();
        if (!isOwner && viewerInvitedOnRequest.Count == 0)
            return null;

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.EnrollmentRequestId == parent.Id, cancellationToken);

        var deadlineHours = DeadlineHours();
        var now = DateTime.UtcNow;
        var canRespondStage = parent.Status == RequestStatus.Pending
            || (parent.Status == RequestStatus.Approved
                && !parent.Course.IsFlexible
                && enrollment == null);

        var invitedStudents = parent.GroupMembers
            .OrderBy(gm => gm.MemberType == GroupMemberType.Own ? 0 : 1)
            .ThenBy(gm => gm.CreatedAt)
            .Select(gm => MapMemberStudent(
                gm.Id,
                gm.StudentId,
                FormatFullName(gm.Student?.User),
                gm.MemberType.ToString(),
                gm.ConfirmationStatus.ToString(),
                gm.CreatedAt,
                gm.CreatedAt.AddHours(deadlineHours),
                gm.ConfirmedAt,
                gm.ConfirmedByUserId,
                visibleStudentIds.Contains(gm.StudentId)))
            .ToList();

        var actionable = isOwner
            ? new List<int>()
            : invitedStudents
                .Where(s => s.IsViewerOwned
                            && string.Equals(s.MemberType, nameof(GroupMemberType.Invited), StringComparison.OrdinalIgnoreCase)
                            && string.Equals(s.Status, nameof(GroupMemberConfirmationStatus.Pending), StringComparison.OrdinalIgnoreCase)
                            && s.RespondByUtc >= now
                            && canRespondStage)
                .Select(s => s.StudentId)
                .Distinct()
                .ToList();

        var pendingInviteStudentIds = invitedStudents
            .Where(s => string.Equals(s.MemberType, nameof(GroupMemberType.Invited), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.Status, nameof(GroupMemberConfirmationStatus.Pending), StringComparison.OrdinalIgnoreCase))
            .Select(s => s.StudentId)
            .ToList();

        var canCancelInvite = isOwner
            && enrollment == null
            && pendingInviteStudentIds.Count > 0
            && (parent.Status == RequestStatus.Pending || parent.Status == RequestStatus.Approved);

        var teacherUser = parent.Course.Teacher?.User;

        return new StudentInvitationDetailDto
        {
            Source = StudentInvitationDetailDto.SourceEnrollmentRequest,
            InvitationId = opened.Id,
            InvitationKey = StudentInvitationDetailDto.FormatInvitationKey(
                StudentInvitationDetailDto.SourceEnrollmentRequest, opened.Id),
            EnrollmentRequestId = parent.Id,
            OpenSessionRequestId = null,
            CourseId = parent.CourseId,
            CourseTitle = parent.Course.Title,
            CourseImageUrl = _mediaUrlResolver.ToPublicUrl(parent.Course.ImageUrl),
            TeacherDisplayName = FormatFullName(teacherUser),
            TeachingModeName = LocalizableEntity.GetLocalizedValue(
                parent.Course.TeachingMode?.NameAr,
                parent.Course.TeachingMode?.NameEn),
            RequestedByUserName = FormatFullName(parent.RequestedByUser),
            ParentStatus = parent.Status.ToString(),
            CreatedAt = opened.CreatedAt,
            RespondByUtc = opened.CreatedAt.AddHours(deadlineHours),
            InvitedStudents = invitedStudents,
            ViewerStudentIds = viewerOnRequest,
            Sessions = MapEnrollmentRequestSessions(parent),
            IsOwner = isOwner,
            CanRespond = actionable.Count > 0,
            ActionableStudentIds = actionable,
            CanCancelInvite = canCancelInvite,
            CancelableInviteStudentIds = canCancelInvite ? pendingInviteStudentIds : new List<int>(),
            CanCancel = isOwner
                && (parent.Status == RequestStatus.Pending || parent.Status == RequestStatus.Approved)
                && (enrollment == null || enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment),
            CanPay = isOwner
                && enrollment != null
                && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment
                && !enrollment.PaidByUserId.HasValue
                && (!enrollment.PaymentDeadline.HasValue || enrollment.PaymentDeadline.Value >= now),
            EnrollmentId = enrollment?.Id,
            EnrollmentStatus = enrollment?.EnrollmentStatus.ToString(),
            AmountDue = enrollment?.AmountDue > 0 ? enrollment.AmountDue : parent.EstimatedTotalPrice,
            PaymentDeadline = enrollment?.PaymentDeadline,
            PayParticipantId = enrollment?.Participants
                .OrderBy(p => p.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefault(),
            RespondPath = Router.StudentEnrollmentRequestMemberResponse
                .Replace("{enrollmentRequestId}", parent.Id.ToString()),
            RespondAcceptDecision = nameof(GroupMemberConfirmationStatus.Confirmed),
            RespondRejectDecision = nameof(GroupMemberConfirmationStatus.Rejected),
        };
    }

    private async Task<StudentInvitationDetailDto?> GetOpenSessionInvitationDetailAsync(
        int userId,
        int invitationId,
        HashSet<int> visibleStudentIds,
        CancellationToken cancellationToken)
    {
        var parent = await _openSessionRequestRepository.GetTableNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.CreatedByGuardian).ThenInclude(g => g!.User)
            .Include(r => r.Student).ThenInclude(s => s.User)
            .Include(r => r.Domain)
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.TargetedTeacher).ThenInclude(t => t!.User)
            .Include(r => r.Sessions).ThenInclude(s => s.TimeSlot)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(
                r => r.Invitations.Any(i => i.Id == invitationId),
                cancellationToken);

        if (parent == null)
            return null;

        var opened = parent.Invitations.FirstOrDefault(i => i.Id == invitationId);
        if (opened == null)
            return null;

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        var isOwner = parent.RequestedByUserId == userId
                      || (parent.CreatedByGuardianId.HasValue
                          && guardian != null
                          && parent.CreatedByGuardianId == guardian.Id);

        var invitedStudentIds = parent.Invitations.Select(i => i.InvitedStudentId).ToHashSet();
        var memberStudentIds = invitedStudentIds.Append(parent.StudentId).ToHashSet();
        var viewerOnRequest = memberStudentIds.Where(visibleStudentIds.Contains).ToList();
        var viewerInvitedOnRequest = invitedStudentIds.Where(visibleStudentIds.Contains).ToList();
        if (!isOwner && viewerInvitedOnRequest.Count == 0)
            return null;

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.SessionRequestId == parent.Id, cancellationToken);

        var deadlineHours = DeadlineHours();
        var now = DateTime.UtcNow;
        var canRespondStage = parent.Status == OpenSessionRequestStatus.PendingInvitations;

        var invitedStudents = new List<InvitationStudentItemDto>
        {
            MapMemberStudent(
                invitationId: 0,
                studentId: parent.StudentId,
                fullName: FormatFullName(parent.Student?.User),
                memberType: nameof(GroupMemberType.Own),
                status: nameof(GroupMemberConfirmationStatus.Confirmed),
                createdAt: parent.CreatedAt,
                respondByUtc: parent.CreatedAt.AddHours(deadlineHours),
                confirmedAt: parent.CreatedAt,
                confirmedByUserId: parent.RequestedByUserId,
                isViewerOwned: visibleStudentIds.Contains(parent.StudentId))
        };
        invitedStudents.AddRange(parent.Invitations
            .OrderBy(i => i.CreatedAt)
            .Select(i => MapMemberStudent(
                i.Id,
                i.InvitedStudentId,
                FormatFullName(i.InvitedStudent?.User),
                nameof(GroupMemberType.Invited),
                i.Status.ToString(),
                i.CreatedAt,
                i.CreatedAt.AddHours(deadlineHours),
                i.RespondedAt,
                confirmedByUserId: null,
                visibleStudentIds.Contains(i.InvitedStudentId))));

        var actionable = isOwner
            ? new List<int>()
            : invitedStudents
                .Where(s => s.IsViewerOwned
                            && string.Equals(s.MemberType, nameof(GroupMemberType.Invited), StringComparison.OrdinalIgnoreCase)
                            && string.Equals(s.Status, nameof(OpenSessionRequestInvitationStatus.Pending), StringComparison.OrdinalIgnoreCase)
                            && s.RespondByUtc >= now
                            && canRespondStage)
                .Select(s => s.StudentId)
                .Distinct()
                .ToList();

        var targetedTeacher = parent.TargetedTeacher?.User;

        return new StudentInvitationDetailDto
        {
            Source = StudentInvitationDetailDto.SourceOpenSessionRequest,
            InvitationId = opened.Id,
            InvitationKey = StudentInvitationDetailDto.FormatInvitationKey(
                StudentInvitationDetailDto.SourceOpenSessionRequest, opened.Id),
            EnrollmentRequestId = null,
            OpenSessionRequestId = parent.Id,
            TeacherDisplayName = FormatFullName(targetedTeacher),
            TitleEn = parent.Subject?.NameEn,
            TitleAr = parent.Subject?.NameAr,
            DomainName = LocalizableEntity.GetLocalizedValue(parent.Domain?.NameAr, parent.Domain?.NameEn),
            SubjectName = LocalizableEntity.GetLocalizedValue(parent.Subject?.NameAr, parent.Subject?.NameEn),
            TeachingModeName = LocalizableEntity.GetLocalizedValue(
                parent.TeachingMode?.NameAr,
                parent.TeachingMode?.NameEn),
            RequestedByUserName = FormatFullName(parent.RequestedByUser),
            ParentStatus = parent.Status.ToString(),
            CreatedAt = opened.CreatedAt,
            RespondByUtc = opened.CreatedAt.AddHours(deadlineHours),
            InvitedStudents = invitedStudents,
            ViewerStudentIds = viewerOnRequest,
            Sessions = MapOpenSessionSessions(parent),
            IsOwner = isOwner,
            CanRespond = actionable.Count > 0,
            ActionableStudentIds = actionable,
            CanCancelInvite = false,
            CancelableInviteStudentIds = new List<int>(),
            CanCancel = isOwner && OsrCancellableStatuses.Contains(parent.Status),
            CanPay = isOwner
                && enrollment != null
                && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment
                && !enrollment.PaidByUserId.HasValue
                && (!enrollment.PaymentDeadline.HasValue || enrollment.PaymentDeadline.Value >= now),
            EnrollmentId = enrollment?.Id,
            EnrollmentStatus = enrollment?.EnrollmentStatus.ToString(),
            AmountDue = enrollment?.AmountDue,
            PaymentDeadline = enrollment?.PaymentDeadline,
            PayParticipantId = enrollment?.Participants
                .OrderBy(p => p.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefault(),
            RespondPath = Router.StudentOpenSessionRequestMemberResponse
                .Replace("{openSessionRequestId}", parent.Id.ToString()),
            RespondAcceptDecision = nameof(OpenSessionRequestInvitationStatus.Accepted),
            RespondRejectDecision = nameof(OpenSessionRequestInvitationStatus.Rejected),
        };
    }

    private static List<InvitationSessionItemDto> MapEnrollmentRequestSessions(CourseEnrollmentRequest parent)
    {
        if (parent.SelectedSessionSlots is { Count: > 0 })
        {
            var courseSessions = (parent.Course.Sessions ?? [])
                .ToDictionary(s => s.SessionNumber, s => s);

            return parent.SelectedSessionSlots
                .OrderBy(s => s.SessionNumber)
                .Select(slot =>
                {
                    var timeSlot = slot.TeacherAvailability?.TimeSlot;
                    courseSessions.TryGetValue(slot.SessionNumber, out var courseSession);
                    return new InvitationSessionItemDto
                    {
                        SequenceNumber = slot.SessionNumber,
                        Date = slot.SessionDate,
                        DurationMinutes = timeSlot?.ResolveDurationMinutes()
                                          ?? courseSession?.DurationMinutes
                                          ?? 0,
                        Title = courseSession?.Title,
                        Notes = courseSession?.Notes,
                        TeacherAvailabilityId = slot.TeacherAvailabilityId,
                        TimeSlotId = timeSlot?.Id,
                        TimeSlotLabelEn = timeSlot?.LabelEn,
                        TimeSlotLabelAr = timeSlot?.LabelAr,
                        StartTime = timeSlot?.StartTime,
                        EndTime = timeSlot?.EndTime,
                        Units = MapSlotUnits(slot.Units),
                    };
                })
                .ToList();
        }

        if (parent.ProposedSessions is { Count: > 0 })
        {
            return parent.ProposedSessions
                .OrderBy(s => s.SessionNumber)
                .Select(ps => new InvitationSessionItemDto
                {
                    SequenceNumber = ps.SessionNumber,
                    DurationMinutes = ps.DurationMinutes,
                    Title = ps.Title,
                    Notes = ps.Notes,
                    Units = MapProposedUnits(ps.Units),
                })
                .ToList();
        }

        return [];
    }

    private static List<InvitationSessionItemDto> MapOpenSessionSessions(OpenSessionRequest parent)
    {
        return (parent.Sessions ?? [])
            .OrderBy(s => s.SequenceNumber)
            .Select(session =>
            {
                var timeSlot = session.TimeSlot;
                return new InvitationSessionItemDto
                {
                    SequenceNumber = session.SequenceNumber,
                    Date = session.PreferredDate,
                    DurationMinutes = session.DurationMinutes > 0
                        ? session.DurationMinutes
                        : timeSlot?.ResolveDurationMinutes() ?? 0,
                    Notes = session.Notes,
                    TimeSlotId = session.TimeSlotId,
                    TimeSlotLabelEn = timeSlot?.LabelEn,
                    TimeSlotLabelAr = timeSlot?.LabelAr,
                    StartTime = timeSlot?.StartTime,
                    EndTime = timeSlot?.EndTime,
                    Units = (session.Units ?? [])
                        .Select(u => new InvitationSessionUnitDto
                        {
                            ContentUnitId = u.ContentUnitId,
                            ContentUnitNameEn = u.ContentUnit?.NameEn,
                            ContentUnitNameAr = u.ContentUnit?.NameAr,
                            LessonId = u.LessonId,
                            LessonNameEn = u.Lesson?.NameEn,
                            LessonNameAr = u.Lesson?.NameAr,
                            CustomUnitLabel = u.CustomUnitLabel,
                            IncludesAllLessons = u.IncludesAllLessons,
                        })
                        .ToList(),
                };
            })
            .ToList();
    }

    private static List<InvitationSessionUnitDto> MapSlotUnits(
        ICollection<CourseRequestSelectedSessionSlotUnit>? units)
    {
        return (units ?? [])
            .Select(u => new InvitationSessionUnitDto
            {
                ContentUnitId = u.ContentUnitId,
                ContentUnitNameEn = u.ContentUnit?.NameEn,
                ContentUnitNameAr = u.ContentUnit?.NameAr,
                LessonId = u.LessonId,
                LessonNameEn = u.Lesson?.NameEn,
                LessonNameAr = u.Lesson?.NameAr,
            })
            .ToList();
    }

    private static List<InvitationSessionUnitDto> MapProposedUnits(
        ICollection<CourseRequestProposedSessionUnit>? units)
    {
        return (units ?? [])
            .Select(u => new InvitationSessionUnitDto
            {
                ContentUnitId = u.ContentUnitId,
                ContentUnitNameEn = u.ContentUnit?.NameEn,
                ContentUnitNameAr = u.ContentUnit?.NameAr,
                LessonId = u.LessonId,
                LessonNameEn = u.Lesson?.NameEn,
                LessonNameAr = u.Lesson?.NameAr,
            })
            .ToList();
    }

    private static InvitationStudentItemDto MapMemberStudent(
        int invitationId,
        int studentId,
        string? fullName,
        string memberType,
        string status,
        DateTime createdAt,
        DateTime respondByUtc,
        DateTime? confirmedAt,
        int? confirmedByUserId,
        bool isViewerOwned)
        => new()
        {
            InvitationId = invitationId,
            StudentId = studentId,
            FullName = fullName,
            MemberType = memberType,
            Status = status,
            CreatedAt = createdAt,
            RespondByUtc = respondByUtc,
            ConfirmedAt = confirmedAt,
            ConfirmedByUserId = confirmedByUserId,
            IsViewerOwned = isViewerOwned,
        };

    private void ApplyInvitationListComputedFields(
        IEnumerable<StudentInvitationListItemDto> items,
        string source,
        int deadlineHours)
    {
        foreach (var item in items)
        {
            if (source == StudentInvitationDetailDto.SourceEnrollmentRequest)
                item.CourseImageUrl = _mediaUrlResolver.ToPublicUrl(item.CourseImageUrl);
            item.RespondByUtc = item.CreatedAt.AddHours(deadlineHours);
            item.InvitationKey = StudentInvitationDetailDto.FormatInvitationKey(source, item.InvitationId);
        }
    }

    private int DeadlineHours() => Math.Max(1, _enrollmentSettings.InviteResponseDeadlineHours);

    private async Task<List<int>> ResolveVisibleStudentIdsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var visible = new List<int>();

        var ownStudent = await _studentRepository.GetByUserIdAsync(userId);
        if (ownStudent != null && !ownStudent.GuardianId.HasValue)
            visible.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            visible.AddRange(children.Select(c => c.Id));
        }

        return visible.Distinct().ToList();
    }

    private static string FormatFullName(User? user)
    {
        if (user == null)
            return string.Empty;

        return string.Join(
            " ",
            new[]
            {
                (user.FirstName ?? string.Empty).Trim(),
                (user.LastName ?? string.Empty).Trim(),
            }.Where(s => !string.IsNullOrEmpty(s)));
    }
}
