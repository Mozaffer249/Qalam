using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.StartMySession;

public class StartMySessionCommandHandler : ResponseHandler,
    IRequestHandler<StartMySessionCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly SessionSettings _sessionSettings;

    public StartMySessionCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseScheduleRepository scheduleRepository,
        IOptions<SessionSettings> sessionSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _scheduleRepository = scheduleRepository;
        _sessionSettings = sessionSettings.Value;
    }

    public async Task<Response<string>> Handle(StartMySessionCommand request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<string>("Session not found.");

        if (!TeacherSessionCommandHelpers.TeacherOwnsSchedule(schedule, teacher.Id))
            return Forbidden<string>("This session does not belong to you.");

        if (schedule.Status == ScheduleStatus.InProgress)
            return Success(entity: "Session already in progress.");

        if (schedule.Status != ScheduleStatus.Scheduled)
            return BadRequest<string>($"Cannot start a session in status {schedule.Status}.");

        if (!TeacherSessionCommandHelpers.CanStartSessionUtc(
                schedule, DateTime.UtcNow, _sessionSettings.EnforceJoinWindow))
            return BadRequest<string>("Session can only be started during its scheduled time window.");

        schedule.Status = ScheduleStatus.InProgress;
        schedule.StartedAt = DateTime.UtcNow;
        await _scheduleRepository.SaveChangesAsync();

        return Success(entity: "Session started.");
    }
}
