using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Commands.ApproveDocument;
using Qalam.Core.Features.Admin.Commands.BlockTeacher;
using Qalam.Core.Features.Admin.Commands.RejectDocument;
using Qalam.Core.Features.Admin.Queries.GetPendingTeachers;
using Qalam.Core.Features.Admin.Queries.GetTeacherDetails;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// Admin endpoints for managing teacher activation and document verification
/// </summary>
[ApiController]
[Route("Api/V1/Admin/[controller]")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class TeacherManagementController : AppControllerBase
{
	private readonly IMediator _mediator;

	public TeacherManagementController(IMediator mediator)
	{
		_mediator = mediator;
	}

	/// <summary>
	/// Get list of teachers pending verification or with rejected documents
	/// </summary>
	/// <param name="pageNumber">Page number (default: 1)</param>
	/// <param name="pageSize">Page size (default: 10)</param>
	[HttpGet("Pending")]
	public async Task<IActionResult> GetPendingTeachers(int pageNumber = 1, int pageSize = 10)
	{
		var query = new GetPendingTeachersQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize
		};
		var response = await _mediator.Send(query);
		return NewResult(response);
	}

	/// <summary>
	/// Get teacher details with all documents for review
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	[HttpGet("{teacherId:int}")]
	public async Task<IActionResult> GetTeacherDetails(int teacherId)
	{
		var query = new GetTeacherDetailsQuery
		{
			TeacherId = teacherId
		};
		var response = await _mediator.Send(query);
		return NewResult(response);
	}

	/// <summary>
	/// Approve a specific document for a teacher
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	/// <param name="documentId">Document ID</param>
	[HttpPost("{teacherId:int}/Documents/{documentId:int}/Approve")]
	public async Task<IActionResult> ApproveDocument(int teacherId, int documentId)
	{
		var command = new ApproveDocumentCommand
		{
			TeacherId = teacherId,
			DocumentId = documentId
			// UserId automatically populated by UserIdentityBehavior
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}

	/// <summary>
	/// Reject a specific document for a teacher with reason
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	/// <param name="documentId">Document ID</param>
	/// <param name="request">Rejection reason</param>
	[HttpPost("{teacherId:int}/Documents/{documentId:int}/Reject")]
	public async Task<IActionResult> RejectDocument(int teacherId, int documentId, [FromBody] RejectDocumentRequest request)
	{
		var command = new RejectDocumentCommand
		{
			TeacherId = teacherId,
			DocumentId = documentId,
			Reason = request.Reason
			// UserId automatically populated by UserIdentityBehavior
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}

	/// <summary>
	/// Block a teacher account
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	/// <param name="request">Block reason (optional)</param>
	[HttpPost("{teacherId:int}/Block")]
	public async Task<IActionResult> BlockTeacher(int teacherId, [FromBody] RejectDocumentRequest? request)
	{
		var command = new BlockTeacherCommand
		{
			TeacherId = teacherId,
			Reason = request?.Reason
			// UserId automatically populated by UserIdentityBehavior
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}
}
