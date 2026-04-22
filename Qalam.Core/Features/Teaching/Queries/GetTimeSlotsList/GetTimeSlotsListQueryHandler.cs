using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Queries.GetTimeSlotsList;

public class GetTimeSlotsListQueryHandler : ResponseHandler,
    IRequestHandler<GetTimeSlotsListQuery, Response<List<TimeSlot>>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public GetTimeSlotsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeachingConfigurationService teachingService) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<List<TimeSlot>>> Handle(
        GetTimeSlotsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teachingService.GetPaginatedTimeSlotsAsync(
            request.PageNumber,
            request.PageSize);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
