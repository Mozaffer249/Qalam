using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Commons;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionById;

public class GetStudentSessionByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentSessionByIdQuery, Response<StudentSessionDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ISessionReviewService _reviewService;
    private readonly SessionSettings _sessionSettings;

    public GetStudentSessionByIdQueryHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ICourseScheduleRepository scheduleRepository,
        ISessionReviewService reviewService,
        IOptions<SessionSettings> sessionSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _scheduleRepository = scheduleRepository;
        _reviewService = reviewService;
        _sessionSettings = sessionSettings.Value;
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
        if (viewingStudentId is int studentId)
        {
            attendance = schedule.Attendances?.FirstOrDefault(a => a.StudentId == studentId);
        }

        var reviews = await _reviewService.GetReviewsForSessionAsync(schedule.Id, cancellationToken);
        if (viewingStudentId is int filterStudentId)
        {
            reviews = reviews
                .Where(r => r.StudentId == filterStudentId)
                .ToList();
        }
        else
        {
            reviews = [];
        }

        var dto = new StudentSessionDetailDto
        {
            ScheduleId = schedule.Id,
            EnrollmentId = enrollment.Id,
            SessionNumber = sessionNumber,
            Title = title,
            Notes = proposed?.Notes ?? courseSession?.Notes,
            TeacherNote = schedule.TeacherNote,
            Date = schedule.Date,
            StartTime = slot?.StartTime,
            EndTime = slot?.EndTime,
            DurationMinutes = duration,
            ActualDurationMinutes = ResolveActualDurationMinutes(schedule),
            Status = schedule.Status,
            CanJoin = canJoin,
            AttendanceStatus = attendance?.Status.ToString(),
            Units = MapUnits(courseSession?.Units),
            Reviews = reviews,
        };

        return Success(entity: dto);
    }

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

        return enrollment.Participants.FirstOrDefault()?.StudentId
               ?? enrollment.LeaderStudentId;
    }

    private Task<CourseSchedule?> LoadScheduleAsync(int id, CancellationToken cancellationToken)
    {
        return _scheduleRepository.GetTableNoTracking()
            .AsSplitQuery()
            .Include(cs => cs.Enrollment)
                .ThenInclude(e => e.Participants)
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
