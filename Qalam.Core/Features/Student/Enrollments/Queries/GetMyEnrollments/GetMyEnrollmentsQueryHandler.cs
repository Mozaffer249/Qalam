using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;

public class GetMyEnrollmentsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentsQuery, Response<List<EnrollmentListItemDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentsQueryHandler(
        IStudentRepository studentRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<Response<List<EnrollmentListItemDto>>> Handle(
        GetMyEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<List<EnrollmentListItemDto>>("Student not found.");

        var query = _enrollmentRepository.GetByStudentIdQueryable(student.Id);
        var totalCount = await query.CountAsync(cancellationToken);

        var enrollments = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<EnrollmentListItemDto>>(enrollments);
        var utcNow = DateTime.UtcNow;

        for (var i = 0; i < enrollments.Count; i++)
            EnrichListItem(items[i], enrollments[i], utcNow);

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, totalCount));
    }

    private static void EnrichListItem(
        EnrollmentListItemDto dto,
        Enrollment enrollment,
        DateTime utcNow)
    {
        var schedules = enrollment.CourseSchedules ?? [];

        var completed = schedules.Count(s => s.Status == ScheduleStatus.Completed);
        dto.CompletedSessionsCount = completed;

        if (dto.SessionsCount is int sessionsTotal && sessionsTotal > 0)
            dto.ProgressPercent = (int)Math.Round(completed * 100.0 / sessionsTotal);
        else
            dto.ProgressPercent = null;

        dto.TeacherIsOnline = schedules.Any(s => s.TeacherInRoom);

        var next = ResolveNextSchedule(schedules, utcNow);
        if (next == null)
        {
            dto.NextSessionAt = null;
            dto.NextScheduleId = null;
            return;
        }

        dto.NextScheduleId = next.Id;
        dto.NextSessionAt = ResolveScheduleStartUtc(next);
    }

    /// <summary>
    /// Prefer InProgress; otherwise earliest upcoming Scheduled (start &gt;= now);
    /// if none upcoming, earliest InProgress/Scheduled by start (overdue still surfaces).
    /// </summary>
    private static CourseSchedule? ResolveNextSchedule(
        IEnumerable<CourseSchedule> schedules,
        DateTime utcNow)
    {
        var actionable = schedules
            .Where(s => s.Status is ScheduleStatus.InProgress or ScheduleStatus.Scheduled)
            .Select(s => (Schedule: s, Start: ResolveScheduleStartUtc(s)))
            .OrderBy(x => x.Start)
            .ToList();

        if (actionable.Count == 0)
            return null;

        var inProgress = actionable
            .Where(x => x.Schedule.Status == ScheduleStatus.InProgress)
            .OrderBy(x => x.Start)
            .Select(x => x.Schedule)
            .FirstOrDefault();
        if (inProgress != null)
            return inProgress;

        var upcoming = actionable
            .Where(x => x.Start >= utcNow)
            .Select(x => x.Schedule)
            .FirstOrDefault();
        if (upcoming != null)
            return upcoming;

        return actionable[0].Schedule;
    }

    private static DateTime ResolveScheduleStartUtc(CourseSchedule schedule)
    {
        var startTime = schedule.TeacherAvailability?.TimeSlot?.StartTime ?? TimeSpan.Zero;
        return PlatformTime.ToUtc(schedule.Date, startTime);
    }
}
