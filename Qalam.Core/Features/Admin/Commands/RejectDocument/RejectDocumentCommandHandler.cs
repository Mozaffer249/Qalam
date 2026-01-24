using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.RejectDocument;

public class RejectDocumentCommandHandler : ResponseHandler,
	IRequestHandler<RejectDocumentCommand, Response<string>>
{
	private readonly ITeacherDocumentRepository _documentRepository;
	private readonly ITeacherRepository _teacherRepository;
	private readonly ILogger<RejectDocumentCommandHandler> _logger;

	public RejectDocumentCommandHandler(
		ITeacherDocumentRepository documentRepository,
		ITeacherRepository teacherRepository,
		ILogger<RejectDocumentCommandHandler> logger,
		IStringLocalizer<SharedResources> localizer) : base(localizer)
	{
		_documentRepository = documentRepository;
		_teacherRepository = teacherRepository;
		_logger = logger;
	}

	public async Task<Response<string>> Handle(
		RejectDocumentCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation(
				"Admin {AdminId} attempting to reject document {DocumentId} for teacher {TeacherId} with reason: {Reason}",
				request.UserId,
				request.DocumentId,
				request.TeacherId,
				request.Reason);

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

			// Update document status to rejected
			document.VerificationStatus = DocumentVerificationStatus.Rejected;
			document.ReviewedByAdminId = request.UserId;
			document.ReviewedAt = DateTime.UtcNow;
			document.RejectionReason = request.Reason;

			await _documentRepository.UpdateAsync(document);
			await _documentRepository.SaveChangesAsync();

			_logger.LogInformation(
				"Document {DocumentId} rejected by admin {AdminId}: {Reason}",
				request.DocumentId,
				request.UserId,
				request.Reason);

			// Update teacher status to DocumentsRejected
			await _teacherRepository.UpdateStatusAsync(request.TeacherId, TeacherStatus.DocumentsRejected);
			await _teacherRepository.SaveChangesAsync();

			_logger.LogInformation(
				"Teacher {TeacherId} status updated to DocumentsRejected",
				request.TeacherId);

			return Success<string>("Document rejected successfully. Teacher will be notified to re-upload.");
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Error rejecting document {DocumentId} for teacher {TeacherId}",
				request.DocumentId,
				request.TeacherId);
			return BadRequest<string>("Failed to reject document");
		}
	}
}
