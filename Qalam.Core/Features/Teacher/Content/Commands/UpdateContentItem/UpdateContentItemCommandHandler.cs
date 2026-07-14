using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.UpdateContentItem;

public class UpdateContentItemCommandHandler : ResponseHandler,
    IRequestHandler<UpdateContentItemCommand, Response<TeacherContentItemDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public UpdateContentItemCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherContentItemDto>> Handle(
        UpdateContentItemCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherContentItemDto>("Teacher profile not found");

        var item = await _contentService.UpdateItemAsync(teacher.Id, request.Id, request.Data, cancellationToken);
        if (item == null)
            return NotFound<TeacherContentItemDto>("Item not found.");
        return Success(entity: item);
    }
}
