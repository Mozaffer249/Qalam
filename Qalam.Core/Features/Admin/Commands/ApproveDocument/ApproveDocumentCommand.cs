using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveDocument;

/// <summary>
/// Command to approve a teacher's document
/// </summary>
public class ApproveDocumentCommand : IRequest<Response<string>>, IAuthenticatedRequest
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
	/// Document ID to approve
	/// </summary>
	public int DocumentId { get; set; }
}
