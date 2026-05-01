using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollmentById;

public class GetMyEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentByIdQuery, Response<EnrollmentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentByIdQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<Response<EnrollmentDetailDto>> Handle(
        GetMyEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<EnrollmentDetailDto>("Student not found.");

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course)
                .ThenInclude(c => c.SessionType)
            .Include(e => e.Course)
                .ThenInclude(c => c.Sessions)
            .Include(e => e.EnrollmentRequest!)
                .ThenInclude(r => r.ProposedSessions)
            .Include(e => e.EnrollmentRequest!)
                .ThenInclude(r => r.SelectedSessionSlots)
            .Include(e => e.ApprovedByTeacher)
                .ThenInclude(t => t.User)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta.TimeSlot)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enrollment == null || enrollment.StudentId != student.Id)
            return NotFound<EnrollmentDetailDto>("Enrollment not found.");

        var dto = _mapper.Map<EnrollmentDetailDto>(enrollment);

        var utcNow = DateTime.UtcNow;
        var schedules = enrollment.CourseSchedules != null
            ? enrollment.CourseSchedules
                .OrderBy(cs => cs.Date)
                .ThenBy(cs => cs.TeacherAvailability != null && cs.TeacherAvailability.TimeSlot != null
                    ? cs.TeacherAvailability.TimeSlot.StartTime
                    : TimeSpan.Zero)
                .ToList()
            : [];

        dto.Sessions = [];
        for (var i = 0; i < schedules.Count; i++)
        {
            var cs = schedules[i];
            var slot = cs.TeacherAvailability?.TimeSlot;
            var duration = cs.DurationMinutes > 0
                ? cs.DurationMinutes
                : slot?.ResolveDurationMinutes() ?? 0;

            dto.Sessions.Add(new EnrollmentSessionItemDto
            {
                ScheduleId = cs.Id,
                Date = cs.Date,
                Title = ResolveSessionTitle(cs, i + 1, enrollment.EnrollmentRequest, enrollment.Course)
                    ?? slot?.LabelEn ?? slot?.LabelAr,
                StartTime = slot != null ? slot.StartTime : null,
                EndTime = slot != null ? slot.EndTime : null,
                DurationMinutes = duration,
                Status = cs.Status,
                CanStart = CanStartSessionUtc(enrollment.EnrollmentStatus, cs.Status, slot, cs.Date, utcNow)
            });
        }

        return Success(entity: dto);
    }

    /// <summary>
    /// Session window uses calendar <paramref name="sessionDate"/> + time slot bounds as UTC instants (same convention as availability calendar).
    /// </summary>
    private static bool CanStartSessionUtc(
        EnrollmentStatus enrollmentStatus,
        ScheduleStatus scheduleStatus,
        TimeSlot? timeSlot,
        DateOnly sessionDate,
        DateTime utcNow)
    {
        if (enrollmentStatus != EnrollmentStatus.Active)
            return false;
        if (scheduleStatus != ScheduleStatus.Scheduled)
            return false;
        if (timeSlot == null)
            return false;

        var start = TimeOnly.FromTimeSpan(timeSlot.StartTime);
        var end = TimeOnly.FromTimeSpan(timeSlot.EndTime);
        if (end <= start)
            return false;

        var startUtc = sessionDate.ToDateTime(start, DateTimeKind.Utc);
        var endUtc = sessionDate.ToDateTime(end, DateTimeKind.Utc);

        return utcNow >= startUtc && utcNow <= endUtc;
    }

    /// <summary>
    /// Prefer matching <see cref="CourseRequestSelectedSessionSlot"/> by date + availability to get <see cref="CourseRequestSelectedSessionSlot.SessionNumber"/>,
    /// then title from proposed session or course template; fallback to ordinal session number in schedule order.
    /// </summary>
    private static string? ResolveSessionTitle(
        CourseSchedule schedule,
        int ordinalSessionNumber,
        CourseEnrollmentRequest? request,
        Course? course)
    {
        var sessionNumber = ordinalSessionNumber;
        var slotMatch = request?.SelectedSessionSlots?
            .FirstOrDefault(ss =>
                ss.SessionDate == schedule.Date && ss.TeacherAvailabilityId == schedule.TeacherAvailabilityId);
        if (slotMatch != null)
            sessionNumber = slotMatch.SessionNumber;

        var proposedTitle = request?.ProposedSessions?
            .FirstOrDefault(p => p.SessionNumber == sessionNumber)?.Title;
        if (!string.IsNullOrWhiteSpace(proposedTitle))
            return proposedTitle;

        var courseTitle = course?.Sessions?
            .FirstOrDefault(s => s.SessionNumber == sessionNumber)?.Title;
        return string.IsNullOrWhiteSpace(courseTitle) ? null : courseTitle;
    }
}
