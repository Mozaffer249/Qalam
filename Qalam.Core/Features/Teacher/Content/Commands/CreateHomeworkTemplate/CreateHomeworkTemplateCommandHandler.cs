using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.CreateHomeworkTemplate;

public class CreateHomeworkTemplateCommandHandler : ResponseHandler,
    IRequestHandler<CreateHomeworkTemplateCommand, Response<TeacherContentItemDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public CreateHomeworkTemplateCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<TeacherContentItemDto>> Handle(
        CreateHomeworkTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherContentItemDto>("Teacher profile not found");

        var item = await _contentService.CreateHomeworkAsync(teacher.Id, request.Data, cancellationToken);
        if (item == null)
            return BadRequest<TeacherContentItemDto>("Invalid homework data.");
        return Success(entity: item);
    }
}
