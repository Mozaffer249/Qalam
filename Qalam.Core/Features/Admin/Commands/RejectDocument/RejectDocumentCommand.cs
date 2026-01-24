using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.RejectDocument;

/// <summary>
/// Command to reject a teacher's document with a reason
/// </summary>
public class RejectDocumentCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
	/// <summary>
	/// Admin ID - automatically populated from JWT token by UserIdentityBehavior
	/// Should NOT be sent by client
	/// </summary>
	[BindNever]
	public int UserId { get; set; }

	/// <summary>
	/// Teacher ID who owns the document
	/// </summary>
	public int TeacherId { get; set; }

	/// <summary>
	/// Document ID to reject
	/// </summary>
	public int DocumentId { get; set; }

	/// <summary>
	/// Reason for rejection (required)
	/// </summary>
	[Required]
	[MaxLength(500)]
	public string Reason { get; set; } = null!;
}
