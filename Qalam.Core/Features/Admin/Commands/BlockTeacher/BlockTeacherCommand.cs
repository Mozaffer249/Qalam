using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.BlockTeacher;

/// <summary>
/// Command to block a teacher account
/// </summary>
public class BlockTeacherCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
	/// <summary>
	/// Admin ID - automatically populated from JWT token by UserIdentityBehavior
	/// Should NOT be sent by client
	/// </summary>
	[BindNever]
	public int UserId { get; set; }

	/// <summary>
	/// Teacher ID to block
	/// </summary>
	public int TeacherId { get; set; }

	/// <summary>
	/// Optional reason for blocking the teacher
	/// </summary>
	[MaxLength(500)]
	public string? Reason { get; set; }
}
