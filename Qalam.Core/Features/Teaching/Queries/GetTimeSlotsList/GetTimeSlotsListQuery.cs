using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Common;

namespace Qalam.Core.Features.Teaching.Queries.GetTimeSlotsList;

public class GetTimeSlotsListQuery : IRequest<Response<List<TimeSlot>>>
{
    public int PageNumber { get; set; } = 1;

    /// <summary>Default 100 so catalog pickers are not truncated.</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// When true, only active slots. When false, only inactive.
    /// When null/omitted, all slots (admin catalog). Teacher pickers should pass true.
    /// </summary>
    public bool? ActiveOnly { get; set; }
}
