using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.CompleteMySession;

public class CompleteMySessionCommandHandler : ResponseHandler,
    IRequestHandler<CompleteMySessionCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ISessionLifecycleService _lifecycleService;

    public CompleteMySessionCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseScheduleRepository scheduleRepository,
        ISessionLifecycleService lifecycleService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _scheduleRepository = scheduleRepository;
        _lifecycleService = lifecycleService;
    }

    public async Task<Response<string>> Handle(CompleteMySessionCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<string>("Session not found.");

        if (!TeacherSessionCommandHelpers.TeacherOwnsSchedule(schedule, teacher.Id))
            return Forbidden<string>("This session does not belong to you.");

        if (schedule.Status is ScheduleStatus.Cancelled or ScheduleStatus.Rescheduled)
            return BadRequest<string>($"Cannot complete a session in status {schedule.Status}.");

        if (schedule.Status == ScheduleStatus.Completed)
            return Success(entity: "Session already completed.");

        await _lifecycleService.CompleteAsync(schedule, cancellationToken);
        return Success(entity: "Session completed.");
    }
}
