using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

/// <summary>
/// Complete academic profile (Domain, Curriculum, Level, Grade) for student or parent who studies
/// </summary>
public class CompleteStudentProfileCommand : IRequest<Response<StudentRegistrationResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public StudentAcademicProfileDto Profile { get; set; } = null!;
}
