using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Authentication.Queries.GetTeacherRegistrationRequirements;

public class GetTeacherRegistrationRequirementsQuery
    : IRequest<Response<TeacherRegistrationRequirementsResponseDto>>, IAuthenticatedRequest
{
    /// <summary>Populated from JWT when present; anonymous catalog callers leave this 0.</summary>
    [BindNever]
    public int UserId { get; set; }
}
