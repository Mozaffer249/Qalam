using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.RescheduleMySession;

public class RescheduleMySessionCommandHandler : ResponseHandler,
    IRequestHandler<RescheduleMySessionCommand, Response<RescheduleMySessionResultDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ITeacherAvailabilityRepository _availabilityRepository;

    public RescheduleMySessionCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseScheduleRepository scheduleRepository,
        ITeacherAvailabilityRepository availabilityRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _scheduleRepository = scheduleRepository;
        _availabilityRepository = availabilityRepository;
    }

    public async Task<Response<RescheduleMySessionResultDto>> Handle(
        RescheduleMySessionCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<RescheduleMySessionResultDto>("Teacher profile not found.");

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<RescheduleMySessionResultDto>("Session not found.");

        if (!TeacherSessionCommandHelpers.TeacherOwnsSchedule(schedule, teacher.Id))
            return Forbidden<RescheduleMySessionResultDto>("This session does not belong to you.");

        if (schedule.Status is not (ScheduleStatus.Scheduled or ScheduleStatus.InProgress))
            return BadRequest<RescheduleMySessionResultDto>(
                $"Cannot reschedule a session in status {schedule.Status}.");

        var availability = await _availabilityRepository.GetTableNoTracking()
            .Include(a => a.TimeSlot)
            .FirstOrDefaultAsync(a => a.Id == request.TeacherAvailabilityId, cancellationToken);

        if (availability == null || !availability.IsActive)
            return BadRequest<RescheduleMySessionResultDto>("Teacher availability slot not found.");

        if (availability.TeacherId != teacher.Id)
            return Forbidden<RescheduleMySessionResultDto>("Availability slot does not belong to you.");

        var duration = availability.TimeSlot.DurationMinutes > 0
            ? availability.TimeSlot.DurationMinutes
            : (int)(availability.TimeSlot.EndTime - availability.TimeSlot.StartTime).TotalMinutes;

        var transaction = await _scheduleRepository.BeginTransactionAsync();
        try
        {
            schedule.Status = ScheduleStatus.Rescheduled;

            var replacement = new CourseSchedule
            {
                EnrollmentId = schedule.EnrollmentId,
                CourseSessionId = schedule.CourseSessionId,
                Date = request.NewDate,
                TeacherAvailabilityId = request.TeacherAvailabilityId,
                DurationMinutes = duration > 0 ? duration : schedule.DurationMinutes,
                TeachingModeId = schedule.TeachingModeId,
                LocationId = schedule.LocationId,
                Status = ScheduleStatus.Scheduled,
            };

            await _scheduleRepository.AddAsync(replacement);
            await _scheduleRepository.SaveChangesAsync();
            await _scheduleRepository.CommitAsync();

            return Success(entity: new RescheduleMySessionResultDto
            {
                OriginalScheduleId = schedule.Id,
                NewScheduleId = replacement.Id,
            });
        }
        catch
        {
            await _scheduleRepository.RollBackAsync();
            throw;
        }
    }
}
