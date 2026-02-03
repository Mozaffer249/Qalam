using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.AddAvailabilityException;

/// <summary>
/// Command to add an availability exception (holiday or extra time)
/// </summary>
public class AddAvailabilityExceptionCommand : IRequest<Response<AvailabilityExceptionResponseDto>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// Date of the exception
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Time slot ID
    /// </summary>
    public int TimeSlotId { get; set; }

    /// <summary>
    /// Type of exception (1 = Blocked/Holiday, 2 = Extra Time)
    /// </summary>
    public AvailabilityExceptionType ExceptionType { get; set; }

    /// <summary>
    /// Optional reason
    /// </summary>
    public string? Reason { get; set; }
}
