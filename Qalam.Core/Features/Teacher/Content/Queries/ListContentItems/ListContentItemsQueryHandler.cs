using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListContentItems;

public class ListContentItemsQueryHandler : ResponseHandler,
    IRequestHandler<ListContentItemsQuery, Response<List<TeacherContentItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public ListContentItemsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<List<TeacherContentItemDto>>> Handle(
        ListContentItemsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<TeacherContentItemDto>>("Teacher profile not found");

        var items = await _contentService.ListItemsAsync(
            teacher.Id,
            request.FolderId,
            request.Kind,
            request.Search,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        return Success(entity: items);
    }
}
