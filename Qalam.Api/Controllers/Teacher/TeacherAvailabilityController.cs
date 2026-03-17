using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Commands.AddAvailabilityException;
using Qalam.Core.Features.Teacher.Commands.DeleteAvailabilityException;
using Qalam.Core.Features.Teacher.Commands.SaveTeacherAvailability;
using Qalam.Core.Features.Teacher.Queries.GetTeacherAvailability;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher endpoints for managing availability schedule and exceptions
/// </summary>
[ApiController]
[Route("Api/V1/Teacher/[controller]")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherAvailabilityController : AppControllerBase
{
    /// <summary>
    /// Get teacher availability (weekly schedule + exceptions)
    /// </summary>
    /// <param name="fromDate">Optional: filter exceptions from this date (default: today)</param>
    /// <param name="toDate">Optional: filter exceptions to this date (default: 90 days from now)</param>
    /// <returns>Teacher weekly schedule and exceptions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TeacherAvailabilityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherAvailability([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        // UserId is auto-populated by UserIdentityBehavior from JWT token
        var query = new GetTeacherAvailabilityQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Add to teacher weekly availability schedule (skips duplicates)
    /// </summary>
    /// <remarks>
    /// This endpoint adds new day/slot combinations to the existing schedule. Duplicate slots are skipped.
    /// 
    /// Example:
    /// ```json
    /// {
    ///   "daySchedules": [
    ///     {
    ///       "dayOfWeekId": 1,
    ///       "timeSlotIds": [1, 2, 3]
    ///     },
    ///     {
    ///       "dayOfWeekId": 3,
    ///       "timeSlotIds": [4, 5]
    ///     }
    ///   ]
    /// }
    /// ```
    /// 
    /// DayOfWeekId: 1=Sunday, 2=Monday, ..., 7=Saturday
    /// </remarks>
    /// <param name="dto">Weekly schedule to save</param>
    /// <returns>Saved weekly schedule with exceptions</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherAvailabilityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveTeacherAvailability([FromBody] SaveTeacherAvailabilityDto dto)
    {
        // UserId is auto-populated by UserIdentityBehavior from JWT token
        var command = new SaveTeacherAvailabilityCommand
        {
            DaySchedules = dto.DaySchedules
        };

        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Add an availability exception (holiday or extra time)
    /// </summary>
    /// <remarks>
    /// ExceptionType values:
    /// - "Blocked" (holiday/unavailable)
    /// - "Extra" (available outside regular schedule)
    ///
    /// Example:
    /// ```json
    /// {
    ///   "date": "2026-02-15",
    ///   "timeSlotId": 5,
    ///   "exceptionType": "Blocked",
    ///   "reason": "Public holiday"
    /// }
    /// ```
    /// </remarks>
    /// <param name="dto">Exception details</param>
    /// <returns>Created exception</returns>
    [HttpPost("exceptions")]
    [ProducesResponseType(typeof(AvailabilityExceptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddException([FromBody] AddAvailabilityExceptionDto dto)
    {
        // UserId is auto-populated by UserIdentityBehavior from JWT token
        var command = new AddAvailabilityExceptionCommand
        {
            Date = dto.Date,
            TimeSlotId = dto.TimeSlotId,
            ExceptionType = dto.ExceptionType,
            Reason = dto.Reason
        };

        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete an availability exception
    /// </summary>
    /// <param name="id">Exception ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("exceptions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteException(int id)
    {
        // UserId is auto-populated by UserIdentityBehavior from JWT token
        var command = new DeleteAvailabilityExceptionCommand
        {
            ExceptionId = id
        };

        return NewResult(await Mediator.Send(command));
    }
}
