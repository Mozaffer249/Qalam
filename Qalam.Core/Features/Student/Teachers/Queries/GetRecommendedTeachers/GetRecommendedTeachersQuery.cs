using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetRecommendedTeachers;

/// <summary>
/// Top-N teachers for the caller's student profile (Domain + Level + Grade narrowing).
/// The card's <c>id</c> plugs straight back into <c>CreateOpenSessionRequestDto.TargetedTeacherId</c>
/// for the Scenario 2 targeted-teacher flow.
/// </summary>
public class GetRecommendedTeachersQuery : IRequest<Response<List<TeacherCardDto>>>, IAuthenticatedRequest
{
    /// <summary>Auto-populated from JWT — do NOT send.</summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// Omit or 0 → use the authenticated user's linked student. Guardians without their own student
    /// profile must pass an explicit child Student.Id.
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>How many cards to return. Defaults to 8.</summary>
    public int? Take { get; set; }
}
