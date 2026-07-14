using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListSessionContent;

public class ListSessionContentQueryHandler : ResponseHandler,
    IRequestHandler<ListSessionContentQuery, Response<List<TeacherSessionContentLinkDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public ListSessionContentQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<List<TeacherSessionContentLinkDto>>> Handle(
        ListSessionContentQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherSessionContentLinkDto>>("Teacher profile not found");

        var links = await _contentService.ListSessionContentAsync(teacher.Id, request.ScheduleId, cancellationToken);
        return Success(entity: links);
    }
}
