using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListContentFolders;

public class ListContentFoldersQueryHandler : ResponseHandler,
    IRequestHandler<ListContentFoldersQuery, Response<List<TeacherContentFolderDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public ListContentFoldersQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<List<TeacherContentFolderDto>>> Handle(
        ListContentFoldersQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherContentFolderDto>>("Teacher profile not found");

        var folders = await _contentService.ListFoldersAsync(teacher.Id, request.ParentId, cancellationToken);
        return Success(entity: folders);
    }
}
