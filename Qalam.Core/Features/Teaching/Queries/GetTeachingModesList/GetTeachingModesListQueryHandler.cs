using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Teaching;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Queries.GetTeachingModesList;

public class GetTeachingModesListQueryHandler : ResponseHandler,
    IRequestHandler<GetTeachingModesListQuery, Response<List<TeachingMode>>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public GetTeachingModesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeachingConfigurationService teachingService) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<List<TeachingMode>>> Handle(
        GetTeachingModesListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teachingService.GetPaginatedTeachingModesAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
