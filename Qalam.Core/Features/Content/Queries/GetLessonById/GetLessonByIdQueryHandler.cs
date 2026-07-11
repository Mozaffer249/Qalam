using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Queries.GetLessonById;

public class GetLessonByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetLessonByIdQuery, Response<Lesson>>
{
    private readonly IContentManagementService _contentService;

    public GetLessonByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<Lesson>> Handle(
        GetLessonByIdQuery request,
        CancellationToken cancellationToken)
    {
        var lesson = await _contentService.GetLessonByIdAsync(request.Id);

        if (lesson == null)
            return NotFound<Lesson>("Lesson not found");

        return Success(entity: lesson);
    }
}
