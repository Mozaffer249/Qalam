using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Commands.CreateLesson;

public class CreateLessonCommandHandler : ResponseHandler,
    IRequestHandler<CreateLessonCommand, Response<Lesson>>
{
    private readonly IContentManagementService _contentService;

    public CreateLessonCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<Lesson>> Handle(
        CreateLessonCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var lesson = new Lesson
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                UnitId = request.UnitId,
                OrderIndex = request.OrderIndex
            };

            var result = await _contentService.CreateLessonAsync(lesson);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Lesson>(ex.Message);
        }
    }
}
