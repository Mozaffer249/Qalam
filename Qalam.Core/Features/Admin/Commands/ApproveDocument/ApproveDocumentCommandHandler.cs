using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveDocument;

public class ApproveDocumentCommandHandler : ResponseHandler,
	IRequestHandler<ApproveDocumentCommand, Response<string>>
{
	private readonly ITeacherDocumentRepository _documentRepository;
	private readonly ITeacherRegistrationCompletionService _completionService;
	private readonly ILogger<ApproveDocumentCommandHandler> _logger;

	public ApproveDocumentCommandHandler(
		ITeacherDocumentRepository documentRepository,
		ITeacherRegistrationCompletionService completionService,
		ILogger<ApproveDocumentCommandHandler> logger,
		IStringLocalizer<SharedResources> localizer) : base(localizer)
	{
		_documentRepository = documentRepository;
		_completionService = completionService;
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

			await _completionService.SyncSubmissionStatusFromDocumentAsync(
				request.DocumentId,
				DocumentVerificationStatus.Approved,
				request.UserId,
				null,
				cancellationToken);

			_logger.LogInformation(
				"Document {DocumentId} approved by admin {AdminId}",
				request.DocumentId,
				request.UserId);

			await _completionService.RefreshTeacherStatusAfterReviewAsync(request.TeacherId, cancellationToken);

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
}
