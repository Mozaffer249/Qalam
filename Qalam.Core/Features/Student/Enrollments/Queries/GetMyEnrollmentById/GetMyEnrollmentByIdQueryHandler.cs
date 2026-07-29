using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Commons;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollmentById;

public class GetMyEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentByIdQuery, Response<EnrollmentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;
    private readonly SessionSettings _sessionSettings;

    public GetMyEnrollmentByIdQueryHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IOptions<SessionSettings> sessionSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
        _sessionSettings = sessionSettings.Value;
    }

    public async Task<Response<EnrollmentDetailDto>> Handle(
        GetMyEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ownedStudentIds = await ResolveOwnedStudentIdsAsync(request.UserId);

        var enrollment = await LoadEnrollmentAsync(request.Id, cancellationToken);
        if (enrollment == null)
            return NotFound<EnrollmentDetailDto>("Enrollment not found.");

        var isOwner = enrollment.OwnerUserId == request.UserId
                      || enrollment.EnrollmentRequest?.RequestedByUserId == request.UserId;
        var isParticipant = enrollment.Participants.Any(p => ownedStudentIds.Contains(p.StudentId));
        if (!isOwner && !isParticipant)
            return NotFound<EnrollmentDetailDto>("Enrollment not found.");

        var dto = _mapper.Map<EnrollmentDetailDto>(enrollment);
        dto.Participants = enrollment.Participants
            .Select(p => _mapper.Map<EnrollmentParticipantDto>(p))
            .ToList();
        dto.IsOwner = isOwner;
        ApplyPaymentFlags(dto, enrollment, isOwner);
        ApplyProgress(dto, enrollment);

        var viewingStudentId = ResolveViewingStudentId(enrollment, ownedStudentIds);
        dto.Sessions = BuildSessions(
            enrollment, viewingStudentId, _sessionSettings.EnforceJoinWindow);

        return Success(entity: dto);
    }

    private static void ApplyProgress(EnrollmentDetailDto dto, Enrollment enrollment)
    {
        var completed = (enrollment.CourseSchedules ?? [])
            .Count(s => s.Status == ScheduleStatus.Completed);
        dto.CompletedSessionsCount = completed;

        if (dto.SessionsCount is int sessionsTotal && sessionsTotal > 0)
            dto.ProgressPercent = (int)Math.Round(completed * 100.0 / sessionsTotal);
        else
            dto.ProgressPercent = null;
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

    private Task<Enrollment?> LoadEnrollmentAsync(int id, CancellationToken cancellationToken)
    {
        return _enrollmentRepository.GetTableNoTracking()
            .AsSplitQuery()
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeachingMode)
            .Include(e => e.Course)
                .ThenInclude(c => c!.SessionType)
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeacherSubject)
                    .ThenInclude(ts => ts.Subject)
                        .ThenInclude(s => s.Domain)
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeacherSubject.Subject.Curriculum)
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeacherSubject.Subject.Level)
            .Include(e => e.Course)
                .ThenInclude(c => c!.TeacherSubject.Subject.Grade)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sessions)
                    .ThenInclude(s => s.Units)
                        .ThenInclude(u => u.ContentUnit)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sessions)
                    .ThenInclude(s => s.Units)
                        .ThenInclude(u => u.Lesson)
            .Include(e => e.EnrollmentRequest!)
                .ThenInclude(r => r.ProposedSessions)
            .Include(e => e.EnrollmentRequest!)
                .ThenInclude(r => r.SelectedSessionSlots)
            .Include(e => e.SelectedSessionSlots)
            .Include(e => e.ApprovedByTeacher)
                .ThenInclude(t => t.User)
            .Include(e => e.LeaderStudent).ThenInclude(s => s!.User)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta!.TimeSlot)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.Attendances)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    private static void ApplyPaymentFlags(
        EnrollmentDetailDto dto,
        Enrollment enrollment,
        bool isOwner)
    {
        dto.AmountDue = enrollment.AmountDue;
        dto.PaymentDeadline = enrollment.PaymentDeadline;

        var now = DateTime.UtcNow;
        var deadlineOk = !enrollment.PaymentDeadline.HasValue
                         || enrollment.PaymentDeadline.Value >= now;
        var alreadyPaid = enrollment.PaidByUserId.HasValue
                          || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded);
        var pendingParticipant = enrollment.Participants
            .FirstOrDefault(p => p.PaymentStatus == PaymentStatus.Pending);

        dto.CanPay = isOwner
                     && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment
                     && deadlineOk
                     && !alreadyPaid
                     && enrollment.AmountDue > 0
                     && pendingParticipant != null;
        dto.PayParticipantId = dto.CanPay ? pendingParticipant!.Id : null;
        dto.CanCancel = isOwner
                        && enrollment.EnrollmentStatus == EnrollmentStatus.PendingPayment;
    }

    private static List<EnrollmentSessionItemDto> BuildSessions(
        Enrollment enrollment,
        int? viewingStudentId,
        bool enforceJoinWindow)
    {
        var utcNow = DateTime.UtcNow;
        var courseSessionsByNumber = (enrollment.Course?.Sessions ?? [])
            .ToDictionary(s => s.SessionNumber, s => s);

        var schedules = (enrollment.CourseSchedules ?? [])
            .OrderBy(cs => cs.Date)
            .ThenBy(cs => cs.TeacherAvailability?.TimeSlot?.StartTime ?? TimeSpan.Zero)
            .ToList();

        if (schedules.Count == 0)
        {
            return courseSessionsByNumber.Values
                .OrderBy(s => s.SessionNumber)
                .Select(s => new EnrollmentSessionItemDto
                {
                    ScheduleId = 0,
                    SessionNumber = s.SessionNumber,
                    Title = s.Title,
                    Notes = s.Notes,
                    DurationMinutes = s.DurationMinutes,
                    Units = MapUnits(s.Units)
                })
                .ToList();
        }

        var sessions = new List<EnrollmentSessionItemDto>(schedules.Count);
        for (var i = 0; i < schedules.Count; i++)
        {
            var schedule = schedules[i];
            var sessionNumber = ResolveSessionNumber(
                schedule, i + 1, enrollment.EnrollmentRequest, enrollment.SelectedSessionSlots);
            courseSessionsByNumber.TryGetValue(sessionNumber, out var courseSession);
            var proposed = enrollment.EnrollmentRequest?.ProposedSessions?
                .FirstOrDefault(p => p.SessionNumber == sessionNumber);

            var slot = schedule.TeacherAvailability?.TimeSlot;
            var duration = schedule.DurationMinutes > 0
                ? schedule.DurationMinutes
                : slot?.ResolveDurationMinutes() ?? courseSession?.DurationMinutes ?? 0;

            var title = proposed?.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = courseSession?.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = slot?.LabelEn ?? slot?.LabelAr;

            var canJoin = SessionJoinRules.CanJoinUtc(
                enrollment.EnrollmentStatus,
                schedule.Status,
                schedule.Date,
                slot?.StartTime,
                slot?.EndTime,
                utcNow,
                enforceJoinWindow);

            SessionAttendance? attendance = null;
            if (viewingStudentId is int studentId)
            {
                attendance = schedule.Attendances?
                    .FirstOrDefault(a => a.StudentId == studentId);
            }

            var (isLocked, unlockAt) = ResolveLock(
                schedule.Status,
                schedule.Date,
                slot?.StartTime,
                utcNow);

            sessions.Add(new EnrollmentSessionItemDto
            {
                ScheduleId = schedule.Id,
                SessionNumber = sessionNumber,
                Date = schedule.Date,
                Title = title,
                Notes = proposed?.Notes ?? courseSession?.Notes,
                StartTime = slot?.StartTime,
                EndTime = slot?.EndTime,
                DurationMinutes = duration,
                ActualDurationMinutes = ResolveActualDurationMinutes(schedule),
                Status = schedule.Status,
                CanStart = canJoin,
                CanJoin = canJoin,
                TeacherAttendanceStatus = schedule.TeacherAttendanceStatus.ToString(),
                TeacherJoinedAt = schedule.TeacherJoinedAt,
                AttendanceStatus = attendance?.Status.ToString(),
                Rating = attendance?.Rating,
                TeacherNote = schedule.TeacherNote,
                IsLocked = isLocked,
                UnlockAt = unlockAt,
                Units = MapUnits(courseSession?.Units)
            });
        }

        return sessions;
    }

    /// <summary>
    /// Sequential date lock: locked when start UTC is strictly after now.
    /// Completed / InProgress / Cancelled never locked. No date → unlocked.
    /// </summary>
    private static (bool IsLocked, DateTime? UnlockAt) ResolveLock(
        ScheduleStatus status,
        DateOnly date,
        TimeSpan? startTime,
        DateTime utcNow)
    {
        if (status is ScheduleStatus.Completed or ScheduleStatus.InProgress or ScheduleStatus.Cancelled)
            return (false, null);

        var startUtc = date.ToDateTime(
            TimeOnly.FromTimeSpan(startTime ?? TimeSpan.Zero),
            DateTimeKind.Utc);

        if (startUtc <= utcNow)
            return (false, null);

        return (true, startUtc);
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

    private static int ResolveSessionNumber(
        CourseSchedule schedule,
        int ordinalSessionNumber,
        CourseEnrollmentRequest? request,
        ICollection<EnrollmentSelectedSessionSlot>? enrollmentSlots)
    {
        var slotMatch = request?.SelectedSessionSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        if (slotMatch != null)
            return slotMatch.SessionNumber;

        var enrollmentSlot = enrollmentSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        return enrollmentSlot?.SessionNumber ?? ordinalSessionNumber;
    }

}
