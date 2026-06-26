using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.ApproveTeacherDomainQuestionSubmission;

public class ApproveTeacherDomainQuestionSubmissionCommandHandler : ResponseHandler,
    IRequestHandler<ApproveTeacherDomainQuestionSubmissionCommand, Response<string>>
{
    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherDomainSubjectCascadeService _cascadeService;

    public ApproveTeacherDomainQuestionSubmissionCommandHandler(
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationCompletionService completionService,
        ITeacherDomainSubjectCascadeService cascadeService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _completionService = completionService;
        _cascadeService = cascadeService;
    }

    public async Task<Response<string>> Handle(
        ApproveTeacherDomainQuestionSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await _submissionRepository.GetByIdWithQuestionAsync(request.SubmissionId, cancellationToken);
        if (submission == null)
            return NotFound<string>("Submission not found");

        if (!submission.Question.RequiresAdminReview)
            return BadRequest<string>("This question does not require admin review.");

        submission.VerificationStatus = DocumentVerificationStatus.Approved;
        submission.ReviewedByAdminId = request.UserId;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.RejectionReason = null;

        await _submissionRepository.UpdateAsync(submission);

        if (submission.TeacherDocumentId.HasValue)
        {
            var doc = await _documentRepository.GetByIdAsync(submission.TeacherDocumentId.Value);
            if (doc != null)
            {
                doc.VerificationStatus = DocumentVerificationStatus.Approved;
                doc.ReviewedByAdminId = request.UserId;
                doc.ReviewedAt = DateTime.UtcNow;
                doc.RejectionReason = null;
                await _documentRepository.UpdateAsync(doc);
            }
        }

        await _submissionRepository.SaveChangesAsync();

        await _cascadeService.ApproveSubjectsInDomainAsync(
            submission.TeacherId,
            submission.Question.DomainId,
            cancellationToken);

        await _completionService.RefreshTeacherStatusAfterReviewAsync(submission.TeacherId, cancellationToken);

        return Success<string>("Domain question submission approved");
    }
}
