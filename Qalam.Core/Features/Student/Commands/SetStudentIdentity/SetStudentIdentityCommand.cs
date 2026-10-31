using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Commands.SetStudentIdentity;

public class SetStudentIdentityCommand : IRequest<Response<StudentIdentityResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public SetStudentIdentityDto Identity { get; set; } = null!;

    /// <summary>
    /// When set, the guardian is updating a child's identity instead of their own.
    /// </summary>
    public int? TargetStudentId { get; set; }
}
