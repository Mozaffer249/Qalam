using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessions;

public class GetMySessionsQueryHandler : ResponseHandler,
    IRequestHandler<GetMySessionsQuery, Response<List<TeacherMySessionListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;

    public GetMySessionsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Response<List<TeacherMySessionListItemDto>>> Handle(
        GetMySessionsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherMySessionListItemDto>>("Teacher not found");

        var items = await _dashboardRepository.GetMySessionsAsync(
            teacher.Id,
            request.Filter,
            request.PageSize,
            cancellationToken);

        return Success(entity: items);
    }
}
