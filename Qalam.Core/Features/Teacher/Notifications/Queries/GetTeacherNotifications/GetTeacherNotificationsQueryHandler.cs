using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Notifications.Queries.GetTeacherNotifications;

public class GetTeacherNotificationsQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherNotificationsQuery, Response<TeacherNotificationsPageDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;

    public GetTeacherNotificationsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Response<TeacherNotificationsPageDto>> Handle(
        GetTeacherNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherNotificationsPageDto>("Teacher not found");

        var page = await _dashboardRepository.GetNotificationsAsync(
            teacher.Id,
            request.UnreadOnly,
            cancellationToken);

        return Success(entity: page);
    }
}
