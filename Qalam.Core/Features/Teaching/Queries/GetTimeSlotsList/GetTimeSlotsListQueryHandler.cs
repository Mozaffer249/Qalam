using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Queries.GetTimeSlotsList;

public class GetTimeSlotsListQueryHandler : ResponseHandler,
    IRequestHandler<GetTimeSlotsListQuery, Response<PaginatedResult<TimeSlot>>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public GetTimeSlotsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeachingConfigurationService teachingService) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<PaginatedResult<TimeSlot>>> Handle(
        GetTimeSlotsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _teachingService.GetPaginatedTimeSlotsAsync(
            request.PageNumber,
            request.PageSize,
            request.DayOfWeek);

        return Success(entity: result);
    }
}
