using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.LinkCourseSessionContentBulk;

public class LinkCourseSessionContentBulkCommandHandler : ResponseHandler,
    IRequestHandler<LinkCourseSessionContentBulkCommand, Response<List<TeacherSessionContentLinkDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public LinkCourseSessionContentBulkCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<List<TeacherSessionContentLinkDto>>> Handle(
        LinkCourseSessionContentBulkCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherSessionContentLinkDto>>("Teacher profile not found");

        var links = await _contentService.LinkCourseSessionContentBulkAsync(
            teacher.Id, request.CourseId, request.SessionId, request.Data, cancellationToken);
        return Success(entity: links);
    }
}
