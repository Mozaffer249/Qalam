using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Common;

namespace Qalam.Core.Features.Teaching.Queries.GetTimeSlotsList;

public class GetTimeSlotsListQuery : IRequest<Response<List<TimeSlot>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
