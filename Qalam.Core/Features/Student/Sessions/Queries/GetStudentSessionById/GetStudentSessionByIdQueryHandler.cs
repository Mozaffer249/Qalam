using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Commons;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Student;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionById;

public class GetStudentSessionByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentSessionByIdQuery, Response<StudentSessionDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ISessionReviewService _reviewService;
    private readonly ITeacherContentService _contentService;
    private readonly SessionSettings _sessionSettings;
    private readonly LiveSessionSettings _liveSessionSettings;

    public GetStudentSessionByIdQueryHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ICourseScheduleRepository scheduleRepository,
        ISessionReviewService reviewService,
        ITeacherContentService contentService,
        IOptions<SessionSettings> sessionSettings,
        IOptions<LiveSessionSettings> liveSessionSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _scheduleRepository = scheduleRepository;
        _reviewService = reviewService;
        _contentService = contentService;
        _sessionSettings = sessionSettings.Value;
        _liveSessionSettings = liveSessionSettings.Value;
    }

    public async Task<Response<StudentSessionDetailDto>> Handle(
        GetStudentSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ownedStudentIds = await ResolveOwnedStudentIdsAsync(request.UserId);

        var schedule = await LoadScheduleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<StudentSessionDetailDto>("Session not found.");

        var enrollment = schedule.Enrollment;
        var isOwner = enrollment.OwnerUserId == request.UserId
                      || enrollment.EnrollmentRequest?.RequestedByUserId == request.UserId;
        var isParticipant = enrollment.Participants.Any(p => ownedStudentIds.Contains(p.StudentId));
        if (!isOwner && !isParticipant)
            return NotFound<StudentSessionDetailDto>("Session not found.");

        // Among many participants, always key per-student fields by this id — never Attendances.First().
        var viewingStudentId = ResolveViewingStudentId(enrollment, ownedStudentIds);
        var slot = schedule.TeacherAvailability?.TimeSlot;
        var courseSession = schedule.CourseSession
                            ?? enrollment.Course?.Sessions?
                                .FirstOrDefault(s => s.Id == schedule.CourseSessionId);
        var sessionNumber = ResolveSessionNumber(schedule, enrollment);

        var proposed = enrollment.EnrollmentRequest?.ProposedSessions?
            .FirstOrDefault(p => p.SessionNumber == sessionNumber);

        var title = proposed?.Title;
        if (string.IsNullOrWhiteSpace(title))
            title = courseSession?.Title;
        if (string.IsNullOrWhiteSpace(title))
            title = slot?.LabelEn ?? slot?.LabelAr;

        var duration = schedule.DurationMinutes > 0
            ? schedule.DurationMinutes
            : slot?.ResolveDurationMinutes() ?? courseSession?.DurationMinutes ?? 0;

        var canJoin = SessionJoinRules.CanJoinUtc(
            enrollment.EnrollmentStatus,
            schedule.Status,
            schedule.Date,
            slot?.StartTime,
            slot?.EndTime,
            DateTime.UtcNow,
            _sessionSettings.EnforceJoinWindow);

        SessionAttendance? attendance = null;
        var isViewingParticipant = false;
        if (viewingStudentId is int studentId)
        {
            isViewingParticipant = enrollment.Participants.Any(p => p.StudentId == studentId);
            attendance = schedule.Attendances?.FirstOrDefault(a => a.StudentId == studentId);
        }

        var startUtc = slot != null
            ? PlatformTime.ToUtc(schedule.Date, slot.StartTime)
            : (DateTime?)null;

        var reviews = await BuildReviewsForViewingStudentAsync(
            schedule.Id, viewingStudentId, cancellationToken);

        SessionReviewDto? ownTeacherReview = null;
        if (viewingStudentId is int reviewStudentId)
        {
            ownTeacherReview = await _reviewService.GetStudentToTeacherReviewAsync(
                schedule.Id, reviewStudentId, cancellationToken);
        }

        var canReview = schedule.Status == ScheduleStatus.Completed
                        && viewingStudentId.HasValue
                        && ownTeacherReview == null;

        var contentLinks = await _contentService.GetContentLinksForSessionAsync(
            schedule.Id, cancellationToken);
        var isOnline = string.Equals(
            schedule.TeachingMode?.Code, "online", StringComparison.OrdinalIgnoreCase);
        var isLive = canJoin || schedule.Status == ScheduleStatus.InProgress;
        var meetingUrl = ResolveMeetingUrl(isOnline, isLive);

        var teacherUser = enrollment.ApprovedByTeacher?.User
                          ?? enrollment.Course?.Teacher?.User;
        var studentUser = viewingStudentId is int vid
            ? enrollment.Participants.FirstOrDefault(p => p.StudentId == vid)?.Student?.User
            : null;

        SessionAttendanceInfoDto? attendanceInfo = null;
        if (isViewingParticipant)
        {
            var (status, isAutoResolved) = SessionAttendanceRules.EffectiveStudentAttendance(attendance);
            attendanceInfo = new SessionAttendanceInfoDto
            {
                Status = status,
                LateMinutes = SessionAttendanceRules.ComputeLateMinutes(attendance?.JoinedAt, startUtc),
                JoinedAt = attendance?.JoinedAt,
                IsAutoResolved = isAutoResolved,
            };
        }

        var (teacherStatus, teacherAuto) = SessionAttendanceRules.EffectiveTeacherAttendance(
            schedule.TeacherAttendanceStatus,
            schedule.TeacherJoinedAt);

        var dto = new StudentSessionDetailDto
        {
            ScheduleId = schedule.Id,
            EnrollmentId = enrollment.Id,
            SessionNumber = sessionNumber,
            Title = title,
            Notes = proposed?.Notes ?? courseSession?.Notes,
            TeacherNote = schedule.TeacherNote,
            TeacherDisplayName = FormatUserName(teacherUser),
            TeacherImageUrl = NullIfEmpty(teacherUser?.ProfilePictureUrl),
            StudentDisplayName = FormatUserName(studentUser),
            StudentAvatarUrl = NullIfEmpty(studentUser?.ProfilePictureUrl),
            Date = schedule.Date,
            StartTime = slot?.StartTime,
            EndTime = slot?.EndTime,
            DurationMinutes = duration,
            ActualDurationMinutes = ResolveActualDurationMinutes(schedule),
            Status = schedule.Status,
            CanJoin = canJoin,
            Attendance = attendanceInfo,
            TeacherAttendance = new SessionAttendanceInfoDto
            {
                Status = teacherStatus,
                LateMinutes = SessionAttendanceRules.ComputeLateMinutes(schedule.TeacherJoinedAt, startUtc),
                JoinedAt = schedule.TeacherJoinedAt,
                IsAutoResolved = teacherAuto,
            },
            CanReview = canReview,
            ReferenceCode = $"CAL-{schedule.Id}",
            RecordingUrl = null,
            StartedAt = ResolveStartedAt(schedule),
            MeetingUrl = meetingUrl,
            Units = MapUnits(courseSession?.Units),
            Attachments = contentLinks.Select(MapAttachment).ToList(),
            Reviews = reviews,
        };

        return Success(entity: dto);
    }

    private async Task<List<SessionReviewDto>> BuildReviewsForViewingStudentAsync(
        int scheduleId,
        int? viewingStudentId,
        CancellationToken cancellationToken)
    {
        if (viewingStudentId is not int studentId)
            return [];

        var reviews = (await _reviewService.GetReviewsForSessionAsync(scheduleId, cancellationToken))
            .Where(r => r.StudentId == studentId)
            .ToList();

        var own = await _reviewService.GetStudentToTeacherReviewAsync(
            scheduleId, studentId, cancellationToken);
        if (own != null && reviews.All(r =>
                !(r.Direction == "StudentToTeacher" && r.Id == own.Id)))
        {
            reviews.Add(own);
        }

        return reviews
            .OrderByDescending(r => r.SubmittedAt)
            .ToList();
    }

    private string? ResolveMeetingUrl(bool isOnline, bool isLive)
    {
        if (!isOnline || !isLive)
            return null;

        var url = _liveSessionSettings.LiveKit?.Url?.Trim();
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    private static DateTime? ResolveStartedAt(CourseSchedule schedule)
    {
        if (schedule.StartedAt.HasValue)
            return schedule.StartedAt;

        if (schedule.Status is ScheduleStatus.InProgress or ScheduleStatus.Completed
            && schedule.TeacherJoinedAt.HasValue)
            return schedule.TeacherJoinedAt;

        return null;
    }

    private static string? FormatUserName(Data.Entity.Identity.User? user)
    {
        if (user == null)
            return null;
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static StudentSessionAttachmentDto MapAttachment(TeacherSessionContentLinkDto link) =>
        new()
        {
            Id = link.Id,
            ContentItemId = link.ContentItemId,
            Title = link.Title,
            Description = link.Description,
            Kind = link.Kind,
            FileType = link.FileType,
            PublicUrl = link.PublicUrl,
        };

    private async Task<HashSet<int>> ResolveOwnedStudentIdsAsync(int userId)
    {
        var ownedStudentIds = new HashSet<int>();
        var ownStudent = await _studentRepository.GetByUserIdAsync(userId);
        if (ownStudent != null)
            ownedStudentIds.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian == null)
            return ownedStudentIds;

        var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
        foreach (var child in children)
            ownedStudentIds.Add(child.Id);

        return ownedStudentIds;
    }

    private static int? ResolveViewingStudentId(Enrollment enrollment, HashSet<int> ownedStudentIds)
    {
        var participant = enrollment.Participants
            .FirstOrDefault(p => ownedStudentIds.Contains(p.StudentId));
        if (participant != null)
            return participant.StudentId;

        if (enrollment.LeaderStudentId is int leaderId && ownedStudentIds.Contains(leaderId))
            return leaderId;

        // Owner without a linked student identity: do not invent a peer's id.
        if (ownedStudentIds.Count == 0)
            return null;

        return null;
    }

    private Task<CourseSchedule?> LoadScheduleAsync(int id, CancellationToken cancellationToken)
    {
        return _scheduleRepository.GetTableNoTracking()
            .AsSplitQuery()
            .Include(cs => cs.TeachingMode)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.Participants)
                    .ThenInclude(p => p.Student)
                        .ThenInclude(s => s.User)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.ApprovedByTeacher!)
                    .ThenInclude(t => t.User)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.EnrollmentRequest!)
                    .ThenInclude(r => r.ProposedSessions)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.EnrollmentRequest!)
                    .ThenInclude(r => r.SelectedSessionSlots)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.SelectedSessionSlots)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.Course!)
                    .ThenInclude(c => c.Teacher!)
                        .ThenInclude(t => t.User)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.Course!)
                    .ThenInclude(c => c.Sessions)
                        .ThenInclude(s => s.Units)
                            .ThenInclude(u => u.ContentUnit)
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.Course!)
                    .ThenInclude(c => c.Sessions)
                        .ThenInclude(s => s.Units)
                            .ThenInclude(u => u.Lesson)
            .Include(cs => cs.CourseSession)
                .ThenInclude(s => s!.Units)
                    .ThenInclude(u => u.ContentUnit)
            .Include(cs => cs.CourseSession)
                .ThenInclude(s => s!.Units)
                    .ThenInclude(u => u.Lesson)
            .Include(cs => cs.Attendances)
            .Include(cs => cs.TeacherAvailability)
                .ThenInclude(ta => ta!.TimeSlot)
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }

    private static int ResolveSessionNumber(CourseSchedule schedule, Enrollment enrollment)
    {
        var request = enrollment.EnrollmentRequest;
        var slotMatch = request?.SelectedSessionSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        if (slotMatch != null)
            return slotMatch.SessionNumber;

        var enrollmentSlot = enrollment.SelectedSessionSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        if (enrollmentSlot != null)
            return enrollmentSlot.SessionNumber;

        if (schedule.CourseSession?.SessionNumber > 0)
            return schedule.CourseSession.SessionNumber;

        return 0;
    }

    private static int? ResolveActualDurationMinutes(CourseSchedule schedule)
    {
        if (schedule.StartedAt is null || schedule.EndedAt is null)
            return null;
        if (schedule.EndedAt <= schedule.StartedAt)
            return null;
        return (int)Math.Round((schedule.EndedAt.Value - schedule.StartedAt.Value).TotalMinutes);
    }

    private static List<EnrollmentSessionContentUnitDto> MapUnits(
        ICollection<CourseSessionUnit>? units)
    {
        if (units == null || units.Count == 0)
            return [];

        return units.Select(u => new EnrollmentSessionContentUnitDto
        {
            Id = u.Id,
            ContentUnitId = u.ContentUnitId,
            ContentUnitName = LocalizableEntity.GetLocalizedValue(
                u.ContentUnit?.NameAr,
                u.ContentUnit?.NameEn),
            LessonId = u.LessonId,
            LessonName = LocalizableEntity.GetLocalizedValue(
                u.Lesson?.NameAr,
                u.Lesson?.NameEn)
        }).ToList();
    }
}
