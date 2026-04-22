using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Queries.GetDaysOfWeekList;

public class GetDaysOfWeekListQueryHandler : ResponseHandler,
    IRequestHandler<GetDaysOfWeekListQuery, Response<List<DayOfWeekDto>>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public GetDaysOfWeekListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeachingConfigurationService teachingService) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<List<DayOfWeekDto>>> Handle(
        GetDaysOfWeekListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teachingService.GetPaginatedDaysOfWeekAsync(
            request.PageNumber,
            request.PageSize);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
