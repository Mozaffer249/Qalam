using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Queries.GetLessonsList;

public class GetLessonsListQueryHandler : ResponseHandler,
    IRequestHandler<GetLessonsListQuery, Response<PaginatedResult<Lesson>>>
{
    private readonly IContentManagementService _contentService;

    public GetLessonsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<PaginatedResult<Lesson>>> Handle(
        GetLessonsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _contentService.GetPaginatedLessonsAsync(
            request.PageNumber,
            request.PageSize,
            request.ContentUnitId,
            request.SubjectId,
            request.Search);

        return Success(entity: result);
    }
}
