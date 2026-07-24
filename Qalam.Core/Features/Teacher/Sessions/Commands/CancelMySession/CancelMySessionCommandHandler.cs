using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.CancelMySession;

public class CancelMySessionCommandHandler : ResponseHandler,
    IRequestHandler<CancelMySessionCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;

    public CancelMySessionCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseScheduleRepository scheduleRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Response<string>> Handle(CancelMySessionCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<string>("Session not found.");

        if (!TeacherSessionCommandHelpers.TeacherOwnsSchedule(schedule, teacher.Id))
            return Forbidden<string>("This session does not belong to you.");

        if (schedule.Status is ScheduleStatus.Completed or ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
            return BadRequest<string>($"Cannot cancel a session in status {schedule.Status}.");

        schedule.Status = ScheduleStatus.Cancelled;
        await _scheduleRepository.SaveChangesAsync();

        return Success(entity: "Session cancelled.");
    }
}
