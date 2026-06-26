using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.RejectTeacherDomainQuestionSubmission;

public class RejectTeacherDomainQuestionSubmissionCommandHandler : ResponseHandler,
    IRequestHandler<RejectTeacherDomainQuestionSubmissionCommand, Response<string>>
{
    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherDomainSubjectCascadeService _cascadeService;
    private readonly ITeacherLifecycleEmailService _lifecycleEmailService;

    public RejectTeacherDomainQuestionSubmissionCommandHandler(
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationCompletionService completionService,
        ITeacherDomainSubjectCascadeService cascadeService,
        ITeacherLifecycleEmailService lifecycleEmailService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _completionService = completionService;
        _cascadeService = cascadeService;
        _lifecycleEmailService = lifecycleEmailService;
    }

    public async Task<Response<string>> Handle(
        RejectTeacherDomainQuestionSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest<string>("Rejection reason is required.");

        var submission = await _submissionRepository.GetByIdWithQuestionAsync(request.SubmissionId, cancellationToken);
        if (submission == null)
            return NotFound<string>("Submission not found");

        if (!submission.Question.RequiresAdminReview)
            return BadRequest<string>("This question does not require admin review.");

        submission.VerificationStatus = DocumentVerificationStatus.Rejected;
        submission.ReviewedByAdminId = request.UserId;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.RejectionReason = request.Reason.Trim();

        await _submissionRepository.UpdateAsync(submission);

        if (submission.TeacherDocumentId.HasValue)
        {
            var doc = await _documentRepository.GetByIdAsync(submission.TeacherDocumentId.Value);
            if (doc != null)
            {
                doc.VerificationStatus = DocumentVerificationStatus.Rejected;
                doc.ReviewedByAdminId = request.UserId;
                doc.ReviewedAt = DateTime.UtcNow;
                doc.RejectionReason = request.Reason.Trim();
                await _documentRepository.UpdateAsync(doc);
            }
        }

        await _submissionRepository.SaveChangesAsync();

        var domainId = submission.Question.DomainId;
        var domainLabel = submission.Question.Domain?.NameEn
            ?? submission.Question.NameEn
            ?? $"Domain {domainId}";

        await _cascadeService.RejectSubjectsInDomainAsync(
            submission.TeacherId,
            domainId,
            request.UserId,
            request.Reason.Trim(),
            cancellationToken);

        await _completionService.RefreshTeacherStatusAfterReviewAsync(submission.TeacherId, cancellationToken);

        await _lifecycleEmailService.SendDomainVerificationRejectedAsync(
            submission.TeacherId,
            domainLabel,
            request.Reason.Trim(),
            cancellationToken);

        return Success<string>("Domain question submission rejected");
    }
}
