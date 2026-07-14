using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.DeleteContentFolder;

public class DeleteContentFolderCommandHandler : ResponseHandler,
    IRequestHandler<DeleteContentFolderCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public DeleteContentFolderCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<string>> Handle(
        DeleteContentFolderCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found");

        var ok = await _contentService.DeleteFolderAsync(teacher.Id, request.Id, cancellationToken);
        if (!ok)
            return BadRequest<string>("Folder not found or not empty.");
        return Success(entity: "Deleted");
    }
}
