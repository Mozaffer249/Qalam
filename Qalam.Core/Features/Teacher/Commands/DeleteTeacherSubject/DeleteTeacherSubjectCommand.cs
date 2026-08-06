using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Commands.DeleteTeacherSubject;

/// <summary>
/// Command to delete a single teacher subject offering.
/// </summary>
public class DeleteTeacherSubjectCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// TeacherSubject ID to delete.
    /// </summary>
    public int Id { get; set; }
}
