using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Queries.GetLessonsList;

public class GetLessonsListQueryHandler : ResponseHandler,
    IRequestHandler<GetLessonsListQuery, Response<List<Lesson>>>
{
    private readonly IContentManagementService _contentService;

    public GetLessonsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<List<Lesson>>> Handle(
        GetLessonsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _contentService.GetPaginatedLessonsAsync(
            request.PageNumber,
            request.PageSize,
            request.ContentUnitId,
            request.SubjectId,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
