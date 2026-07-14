using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.CreateContentFolder;

public class CreateContentFolderCommandHandler : ResponseHandler,
    IRequestHandler<CreateContentFolderCommand, Response<TeacherContentFolderDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public CreateContentFolderCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherContentFolderDto>> Handle(
        CreateContentFolderCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherContentFolderDto>("Teacher profile not found");

        var folder = await _contentService.CreateFolderAsync(teacher.Id, request.Data, cancellationToken);
        if (folder == null)
            return BadRequest<TeacherContentFolderDto>("Invalid folder data or duplicate name.");
        return Success(entity: folder);
    }
}
