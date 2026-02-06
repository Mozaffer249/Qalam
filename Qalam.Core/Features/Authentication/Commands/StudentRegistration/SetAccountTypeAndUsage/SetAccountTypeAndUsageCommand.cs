using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

/// <summary>
/// Set account type and usage (Screen 3 + 4) after VerifyOtp. Creates User profile, roles, Student/Guardian. Validates 18+.
/// </summary>
public class SetAccountTypeAndUsageCommand : IRequest<Response<StudentRegistrationResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public SetAccountTypeAndUsageDto Data { get; set; } = null!;
}
