using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Queries.GetContentUnitsList;

public class GetContentUnitsListQueryHandler : ResponseHandler,
    IRequestHandler<GetContentUnitsListQuery, Response<PaginatedResult<ContentUnit>>>
{
    private readonly IContentManagementService _contentService;

    public GetContentUnitsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<PaginatedResult<ContentUnit>>> Handle(
        GetContentUnitsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _contentService.GetPaginatedContentUnitsAsync(
            request.PageNumber,
            request.PageSize,
            request.SubjectId,
            request.TermIds,
            request.UnitTypeCode,
            request.Search);

        return Success(entity: result);
    }
}
