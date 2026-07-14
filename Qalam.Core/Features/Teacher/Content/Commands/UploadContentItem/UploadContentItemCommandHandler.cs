using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.UploadContentItem;

public class UploadContentItemCommandHandler : ResponseHandler,
    IRequestHandler<UploadContentItemCommand, Response<TeacherContentItemDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public UploadContentItemCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherContentItemDto>> Handle(
        UploadContentItemCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherContentItemDto>("Teacher profile not found");

        var tagList = ParseTags(request.Tags);
        var item = await _contentService.UploadFileAsync(
            teacher.Id,
            request.File,
            request.FolderId,
            request.Title,
            request.Description,
            tagList,
            cancellationToken);
        if (item == null)
            return BadRequest<TeacherContentItemDto>("Invalid file or folder.");
        return Success(entity: item);
    }

    private static List<string>? ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;
        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
