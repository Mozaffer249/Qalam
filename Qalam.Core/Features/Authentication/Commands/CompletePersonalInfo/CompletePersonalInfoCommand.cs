using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Authentication.Commands.CompletePersonalInfo;

public class CompletePersonalInfoCommand : IRequest<Response<TeacherAccountResponseDto>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }
    
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string Password { get; set; } = null!;
}
