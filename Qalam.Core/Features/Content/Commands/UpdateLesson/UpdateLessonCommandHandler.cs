using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Commands.UpdateLesson;

public class UpdateLessonCommandHandler : ResponseHandler,
    IRequestHandler<UpdateLessonCommand, Response<Lesson>>
{
    private readonly IContentManagementService _contentService;

    public UpdateLessonCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<Lesson>> Handle(
        UpdateLessonCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var lesson = new Lesson
            {
                Id = request.Id,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                UnitId = request.UnitId,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive,
                QuranContentTypeId = request.QuranContentTypeId,
                QuranLevelId = request.QuranLevelId
            };

            var result = await _contentService.UpdateLessonAsync(lesson);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Lesson>(ex.Message);
        }
    }
}
