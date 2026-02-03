using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Teacher.Commands.DeleteAvailabilityException;

/// <summary>
/// Command to delete an availability exception
/// </summary>
public class DeleteAvailabilityExceptionCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// Exception ID to delete
    /// </summary>
    public int ExceptionId { get; set; }
}
