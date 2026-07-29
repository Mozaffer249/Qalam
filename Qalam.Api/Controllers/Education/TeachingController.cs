using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teaching.Commands.CreateTimeSlot;
using Qalam.Core.Features.Teaching.Commands.DeleteTimeSlot;
using Qalam.Core.Features.Teaching.Commands.SetTimeSlotActive;
using Qalam.Core.Features.Teaching.Commands.UpdateTimeSlot;
using Qalam.Core.Features.Teaching.Queries.GetDaysOfWeekList;
using Qalam.Core.Features.Teaching.Queries.GetSessionTypesList;
using Qalam.Core.Features.Teaching.Queries.GetTeachingModesList;
using Qalam.Core.Features.Teaching.Queries.GetTimeSlotsList;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teaching;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Teaching configuration: Modes, Session Types, Time Slots, Days of Week
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
    /// Get time slots with pagination. Defaults to active-only; pass <c>activeOnly=</c> empty/null for all (admin).
    /// </summary>
    [HttpGet(Router.TimeSlots)]
    [ProducesResponseType(typeof(List<TimeSlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSlots([FromQuery] GetTimeSlotsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>Create a catalog time slot (Admin).</summary>
    [HttpPost(Router.TimeSlots)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(TimeSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTimeSlot([FromBody] CreateTimeSlotDto data)
    {
        return NewResult(await Mediator.Send(new CreateTimeSlotCommand { Data = data }));
    }

    /// <summary>Update a catalog time slot (Admin).</summary>
    [HttpPut(Router.TimeSlotById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(TimeSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTimeSlot(int id, [FromBody] UpdateTimeSlotDto data)
    {
        return NewResult(await Mediator.Send(new UpdateTimeSlotCommand { Id = id, Data = data }));
    }

    /// <summary>Delete a catalog time slot when unused (Admin). In-use slots must be deactivated.</summary>
    [HttpDelete(Router.TimeSlotById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteTimeSlot(int id)
    {
        return NewResult(await Mediator.Send(new DeleteTimeSlotCommand { Id = id }));
    }

    /// <summary>Set active flag on a catalog time slot (Admin).</summary>
    [HttpPatch(Router.TimeSlots + "/{id:int}/active")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(TimeSlotDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTimeSlotActive(int id, [FromBody] SetTimeSlotActiveDto data)
    {
        return NewResult(await Mediator.Send(new SetTimeSlotActiveCommand { Id = id, Data = data }));
    }

    /// <summary>
    /// Get all days of week with pagination
    /// </summary>
    [HttpGet(Router.DaysOfWeek)]
    public async Task<IActionResult> GetDaysOfWeek([FromQuery] GetDaysOfWeekListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }
}
