using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Teaching;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Queries.GetSessionTypesList;

public class GetSessionTypesListQueryHandler : ResponseHandler,
    IRequestHandler<GetSessionTypesListQuery, Response<List<SessionType>>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public GetSessionTypesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeachingConfigurationService teachingService) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<List<SessionType>>> Handle(
        GetSessionTypesListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teachingService.GetPaginatedSessionTypesAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
