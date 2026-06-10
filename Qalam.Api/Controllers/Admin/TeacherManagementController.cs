using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Commands.ApproveDocument;
using Qalam.Core.Features.Admin.Commands.BlockTeacher;
using Qalam.Core.Features.Admin.Commands.RejectDocument;
using Qalam.Core.Features.Admin.Queries.GetPendingTeachers;
using Qalam.Core.Features.Admin.Queries.GetTeacherDetails;
using Qalam.Core.Features.Admin.Queries.GetTeachersForAdmin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Core.Features.Admin.TeacherSubjects.Commands.ActivateTeacherSubject;
using Qalam.Core.Features.Admin.TeacherSubjects.Commands.InactivateTeacherSubject;
using Qalam.Core.Features.Admin.TeacherSubjects.Commands.RejectTeacherSubject;
using Qalam.Core.Features.Admin.TeacherSubjects.Commands.RestoreTeacherSubject;
using Qalam.Core.Features.Admin.TeacherSubjects.Queries.GetTeacherSubjectByIdForAdmin;
using Qalam.Core.Features.Admin.TeacherSubjects.Queries.GetTeacherSubjectsForAdmin;
using Qalam.Core.Features.Admin.TeacherSubjects.Queries.ListTeacherSubjects;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// Admin endpoints for teacher activation, document verification, and registration requirement review.
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
	/// Paginated list of all teachers with optional filters.
	/// </summary>
	[HttpGet("Teachers")]
	[ProducesResponseType(typeof(List<AdminTeacherListItemDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetTeachers(
		int pageNumber = 1,
		int pageSize = 10,
		string? status = null,
		TeacherLocation? location = null,
		int? subjectId = null,
		string? search = null,
		AdminTeacherListSort sortBy = AdminTeacherListSort.Newest)
	{
		var query = new GetTeachersForAdminQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize,
			Status = status,
			Location = location,
			SubjectId = subjectId,
			Search = search,
			SortBy = sortBy
		};
		var response = await _mediator.Send(query);
		return NewResult(response);
	}

	/// <summary>
	/// Get list of teachers pending verification or with rejected documents.
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
	/// Get teacher details with documents and registration requirement checklist.
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	/// <returns>
	/// `documents` — uploaded files; `registrationRequirements` — per active catalog item with submission status;
	/// `canBeActivated` — true when all required submissions are approved.
	/// </returns>
	/// <remarks>
	/// Activation is based on **active required** registration submissions, not every document row.
	/// See `docs/Teacher-Registration-Guide.md`.
	/// </remarks>
	[HttpGet("{teacherId:int}")]
	[ProducesResponseType(typeof(TeacherDetailsDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
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
	/// Approve a specific document for a teacher.
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	/// <param name="documentId">Document ID</param>
	/// <remarks>
	/// Syncs the linked `TeacherRegistrationSubmission` and refreshes teacher status.
	/// Teacher becomes **Active** when all active required submissions are approved.
	/// </remarks>
	[HttpPost("{teacherId:int}/Documents/{documentId:int}/Approve")]
	[ProducesResponseType(StatusCodes.Status200OK)]
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
	/// Reject a specific document for a teacher with reason.
	/// </summary>
	/// <param name="teacherId">Teacher ID</param>
	/// <param name="documentId">Document ID</param>
	/// <param name="request">Rejection reason shown to the teacher</param>
	/// <remarks>
	/// Syncs the linked submission and sets teacher status to **DocumentsRejected** when a required item is rejected.
	/// </remarks>
	[HttpPost("{teacherId:int}/Documents/{documentId:int}/Reject")]
	[ProducesResponseType(StatusCodes.Status200OK)]
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

	/// <summary>
	/// List teacher subjects across all teachers (admin operations view).
	/// </summary>
	[HttpGet("Subjects")]
	[ProducesResponseType(typeof(List<AdminTeacherSubjectDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> ListTeacherSubjects(
		int pageNumber = 1,
		int pageSize = 10,
		int? teacherId = null,
		int? subjectId = null,
		bool? isActive = null,
		DocumentVerificationStatus? verificationStatus = null)
	{
		var query = new ListTeacherSubjectsQuery
		{
			PageNumber = pageNumber,
			PageSize = pageSize,
			TeacherId = teacherId,
			SubjectId = subjectId,
			IsActive = isActive,
			VerificationStatus = verificationStatus
		};
		var response = await _mediator.Send(query);
		return NewResult(response);
	}

	/// <summary>
	/// Get all teaching subjects for a teacher (active, inactive, and rejected).
	/// </summary>
	[HttpGet("{teacherId:int}/Subjects")]
	[ProducesResponseType(typeof(List<AdminTeacherSubjectDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetTeacherSubjects(int teacherId)
	{
		var query = new GetTeacherSubjectsForAdminQuery { TeacherId = teacherId };
		var response = await _mediator.Send(query);
		return NewResult(response);
	}

	/// <summary>
	/// Get a single teacher subject with units for admin review.
	/// </summary>
	[HttpGet("{teacherId:int}/Subjects/{teacherSubjectId:int}")]
	[ProducesResponseType(typeof(AdminTeacherSubjectDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetTeacherSubject(int teacherId, int teacherSubjectId)
	{
		var query = new GetTeacherSubjectByIdForAdminQuery
		{
			TeacherId = teacherId,
			TeacherSubjectId = teacherSubjectId
		};
		var response = await _mediator.Send(query);
		return NewResult(response);
	}

	/// <summary>
	/// Inactivate a teacher subject (keeps approval status; blocks new courses).
	/// </summary>
	[HttpPost("{teacherId:int}/Subjects/{teacherSubjectId:int}/Inactivate")]
	public async Task<IActionResult> InactivateTeacherSubject(int teacherId, int teacherSubjectId)
	{
		var command = new InactivateTeacherSubjectCommand
		{
			TeacherId = teacherId,
			TeacherSubjectId = teacherSubjectId
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}

	/// <summary>
	/// Activate a previously inactivated teacher subject (not for rejected — use Restore).
	/// </summary>
	[HttpPost("{teacherId:int}/Subjects/{teacherSubjectId:int}/Activate")]
	public async Task<IActionResult> ActivateTeacherSubject(int teacherId, int teacherSubjectId)
	{
		var command = new ActivateTeacherSubjectCommand
		{
			TeacherId = teacherId,
			TeacherSubjectId = teacherSubjectId
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}

	/// <summary>
	/// Reject a teacher subject with reason; deactivates and blocks new courses.
	/// </summary>
	[HttpPost("{teacherId:int}/Subjects/{teacherSubjectId:int}/Reject")]
	public async Task<IActionResult> RejectTeacherSubject(
		int teacherId,
		int teacherSubjectId,
		[FromBody] RejectDocumentRequest request)
	{
		var command = new RejectTeacherSubjectCommand
		{
			TeacherId = teacherId,
			TeacherSubjectId = teacherSubjectId,
			Reason = request.Reason
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}

	/// <summary>
	/// Restore a rejected teacher subject (approved + active, clears rejection reason).
	/// </summary>
	[HttpPost("{teacherId:int}/Subjects/{teacherSubjectId:int}/Restore")]
	public async Task<IActionResult> RestoreTeacherSubject(int teacherId, int teacherSubjectId)
	{
		var command = new RestoreTeacherSubjectCommand
		{
			TeacherId = teacherId,
			TeacherSubjectId = teacherSubjectId
		};
		var response = await _mediator.Send(command);
		return NewResult(response);
	}
}
