using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherAvailability;

/// <summary>
/// Command to save teacher weekly availability schedule
/// </summary>
public class SaveTeacherAvailabilityCommand : IRequest<Response<TeacherAvailabilityResponseDto>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// Weekly schedule (days with time slots)
    /// </summary>
    public List<DayAvailabilityDto> DaySchedules { get; set; } = new();
}
