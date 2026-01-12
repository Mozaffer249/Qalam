using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Queries.GetSessionTypesList;

public class GetSessionTypesListQueryHandler : ResponseHandler,
    IRequestHandler<GetSessionTypesListQuery, Response<PaginatedResult<SessionType>>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public GetSessionTypesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeachingConfigurationService teachingService) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<PaginatedResult<SessionType>>> Handle(
        GetSessionTypesListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teachingService.GetPaginatedSessionTypesAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(entity: result);
    }
}
