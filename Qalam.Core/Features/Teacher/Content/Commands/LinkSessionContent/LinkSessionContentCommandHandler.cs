using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.LinkSessionContent;

public class LinkSessionContentCommandHandler : ResponseHandler,
    IRequestHandler<LinkSessionContentCommand, Response<TeacherSessionContentLinkDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public LinkSessionContentCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherSessionContentLinkDto>> Handle(
        LinkSessionContentCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherSessionContentLinkDto>("Teacher profile not found");

        var link = await _contentService.LinkSessionContentAsync(
            teacher.Id, request.ScheduleId, request.ContentItemId, cancellationToken);
        if (link == null)
            return BadRequest<TeacherSessionContentLinkDto>("Cannot link content.");
        return Success(entity: link);
    }
}
