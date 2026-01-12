using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teaching.Queries.GetSessionTypesList;
using Qalam.Core.Features.Teaching.Queries.GetTeachingModesList;
using Qalam.Core.Features.Teaching.Queries.GetTimeSlotsList;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Teaching configuration: Modes, Session Types, Time Slots
/// </summary>
[Authorize]
public class TeachingController : AppControllerBase
{
    /// <summary>
    /// Get all teaching modes with pagination
    /// </summary>
    [HttpGet(Router.TeachingModes)]
    public async Task<IActionResult> GetTeachingModes([FromQuery] GetTeachingModesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get all session types with pagination
    /// </summary>
    [HttpGet(Router.SessionTypes)]
    public async Task<IActionResult> GetSessionTypes([FromQuery] GetSessionTypesListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get all time slots with pagination and filters
    /// </summary>
    [HttpGet(Router.TimeSlots)]
    public async Task<IActionResult> GetTimeSlots([FromQuery] GetTimeSlotsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }
}
