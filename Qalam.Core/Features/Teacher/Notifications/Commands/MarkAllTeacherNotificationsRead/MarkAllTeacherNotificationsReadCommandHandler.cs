using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Notifications.Commands.MarkAllTeacherNotificationsRead;

public class MarkAllTeacherNotificationsReadCommandHandler : ResponseHandler,
    IRequestHandler<MarkAllTeacherNotificationsReadCommand, Response<int>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;

    public MarkAllTeacherNotificationsReadCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Response<int>> Handle(
        MarkAllTeacherNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<int>("Teacher not found");

        var count = await _dashboardRepository.MarkAllNotificationsReadAsync(teacher.Id, cancellationToken);
        return Success(entity: count);
    }
}
