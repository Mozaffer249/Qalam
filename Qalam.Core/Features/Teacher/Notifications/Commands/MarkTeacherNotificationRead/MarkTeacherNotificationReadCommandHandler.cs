using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Notifications.Commands.MarkTeacherNotificationRead;

public class MarkTeacherNotificationReadCommandHandler : ResponseHandler,
    IRequestHandler<MarkTeacherNotificationReadCommand, Response<bool>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;

    public MarkTeacherNotificationReadCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Response<bool>> Handle(
        MarkTeacherNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<bool>("Teacher not found");

        var ok = await _dashboardRepository.MarkNotificationReadAsync(teacher.Id, request.Id, cancellationToken);
        if (!ok)
            return NotFound<bool>("Notification not found");

        return Success(entity: true);
    }
}
