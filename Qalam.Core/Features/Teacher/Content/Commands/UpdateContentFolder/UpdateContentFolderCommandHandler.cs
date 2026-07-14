using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.UpdateContentFolder;

public class UpdateContentFolderCommandHandler : ResponseHandler,
    IRequestHandler<UpdateContentFolderCommand, Response<TeacherContentFolderDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public UpdateContentFolderCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherContentFolderDto>> Handle(
        UpdateContentFolderCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherContentFolderDto>("Teacher profile not found");

        var folder = await _contentService.UpdateFolderAsync(teacher.Id, request.Id, request.Data, cancellationToken);
        if (folder == null)
            return NotFound<TeacherContentFolderDto>("Folder not found.");
        return Success(entity: folder);
    }
}
