using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveDocument;

public class ApproveDocumentCommandHandler : ResponseHandler,
	IRequestHandler<ApproveDocumentCommand, Response<string>>
{
	private readonly ITeacherDocumentRepository _documentRepository;
	private readonly ITeacherRepository _teacherRepository;
	private readonly ILogger<ApproveDocumentCommandHandler> _logger;

	public ApproveDocumentCommandHandler(
		ITeacherDocumentRepository documentRepository,
		ITeacherRepository teacherRepository,
		ILogger<ApproveDocumentCommandHandler> logger,
		IStringLocalizer<SharedResources> localizer) : base(localizer)
	{
		_documentRepository = documentRepository;
		_teacherRepository = teacherRepository;
		_logger = logger;
	}

	public async Task<Response<string>> Handle(
		ApproveDocumentCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation(
				"Admin {AdminId} attempting to approve document {DocumentId} for teacher {TeacherId}",
				request.UserId,
				request.DocumentId,
				request.TeacherId);

			// Get the document
			var document = await _documentRepository.GetByIdAsync(request.DocumentId);
			if (document == null || document.TeacherId != request.TeacherId)
			{
				_logger.LogWarning(
					"Document {DocumentId} not found for teacher {TeacherId}",
					request.DocumentId,
					request.TeacherId);
				return NotFound<string>("Document not found");
			}

			// Update document status to approved
			document.VerificationStatus = DocumentVerificationStatus.Approved;
			document.ReviewedByAdminId = request.UserId;
			document.ReviewedAt = DateTime.UtcNow;
			document.RejectionReason = null;

			await _documentRepository.UpdateAsync(document);
			await _documentRepository.SaveChangesAsync();

			_logger.LogInformation(
				"Document {DocumentId} approved by admin {AdminId}",
				request.DocumentId,
				request.UserId);

			// Check if all documents are now approved and update teacher status
			await UpdateTeacherStatusAfterReviewAsync(request.TeacherId);

			return Success<string>("Document approved successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Error approving document {DocumentId} for teacher {TeacherId}",
				request.DocumentId,
				request.TeacherId);
			return BadRequest<string>("Failed to approve document");
		}
	}

	/// <summary>
	/// Update teacher status based on document verification states
	/// </summary>
	private async Task UpdateTeacherStatusAfterReviewAsync(int teacherId)
	{
		var documents = await _documentRepository.GetByTeacherIdAsync(teacherId);

		if (!documents.Any())
			return;

		var hasRejected = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Rejected);
		var hasPending = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Pending);
		var allApproved = documents.All(d => d.VerificationStatus == DocumentVerificationStatus.Approved);

		TeacherStatus newStatus;
		if (hasRejected)
		{
			newStatus = TeacherStatus.DocumentsRejected;
		}
		else if (allApproved)
		{
			newStatus = TeacherStatus.Active;
			_logger.LogInformation(
				"All documents approved for teacher {TeacherId}, activating account",
				teacherId);
		}
		else if (hasPending)
		{
			newStatus = TeacherStatus.PendingVerification;
		}
		else
		{
			return; // No change needed
		}

		await _teacherRepository.UpdateStatusAsync(teacherId, newStatus);
		await _teacherRepository.SaveChangesAsync();
	}
}
