using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherAvailability;

/// <summary>
/// Query to get teacher availability (weekly schedule + exceptions)
/// </summary>
public class GetTeacherAvailabilityQuery : IRequest<Response<TeacherAvailabilityResponseDto>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// Optional: filter exceptions from this date (default: today)
    /// </summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>
    /// Optional: filter exceptions to this date (default: 90 days from now)
    /// </summary>
    public DateOnly? ToDate { get; set; }
}
