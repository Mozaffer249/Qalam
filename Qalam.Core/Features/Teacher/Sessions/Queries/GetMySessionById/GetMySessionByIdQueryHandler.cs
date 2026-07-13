using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessionById;

public class GetMySessionByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMySessionByIdQuery, Response<TeacherMySessionDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;
    private readonly ITeacherContentService _contentService;

    public GetMySessionByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherMySessionDetailDto>> Handle(
        GetMySessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherMySessionDetailDto>("Teacher not found");

        var item = await _dashboardRepository.GetMySessionByIdAsync(teacher.Id, request.Id, cancellationToken);
        if (item == null)
            return NotFound<TeacherMySessionDetailDto>("Session not found");

        item.ContentLinks = await _contentService.GetContentLinksForSessionAsync(request.Id, cancellationToken);
        item.Homework = await _contentService.GetHomeworkForSessionAsync(request.Id, cancellationToken);

        return Success(entity: item);
    }
}
