using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationCompletionService : ITeacherRegistrationCompletionService
{
    private readonly ITeacherRegistrationRequirementRepository _requirementRepository;
    private readonly ITeacherRegistrationSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherDomainQuestionRepository _domainQuestionRepository;
    private readonly ITeacherDomainQuestionSubmissionRepository _domainSubmissionRepository;
    private readonly ITeacherLifecycleEmailService _lifecycleEmailService;
    private readonly ILogger<TeacherRegistrationCompletionService> _logger;

    public TeacherRegistrationCompletionService(
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRepository teacherRepository,
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherDomainQuestionRepository domainQuestionRepository,
        ITeacherDomainQuestionSubmissionRepository domainSubmissionRepository,
        ITeacherLifecycleEmailService lifecycleEmailService,
        ILogger<TeacherRegistrationCompletionService> logger)
    {
        _requirementRepository = requirementRepository;
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _teacherRepository = teacherRepository;
        _teacherSubjectRepository = teacherSubjectRepository;
        _domainQuestionRepository = domainQuestionRepository;
        _domainSubmissionRepository = domainSubmissionRepository;
        _lifecycleEmailService = lifecycleEmailService;
        _logger = logger;
    }

    public async Task SyncSubmissionStatusFromDocumentAsync(
        int teacherDocumentId,
        DocumentVerificationStatus status,
        int? reviewedByAdminId,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var submission = await _submissionRepository.GetByTeacherDocumentIdAsync(teacherDocumentId, cancellationToken);
        if (submission != null)
        {
            submission.VerificationStatus = status;
            submission.ReviewedByAdminId = reviewedByAdminId;
            submission.ReviewedAt = reviewedByAdminId.HasValue ? DateTime.UtcNow : submission.ReviewedAt;
            submission.RejectionReason = rejectionReason;

            await _submissionRepository.UpdateAsync(submission);
            await _submissionRepository.SaveChangesAsync();
            return;
        }

        var domainSubmission = await _domainSubmissionRepository.GetByTeacherDocumentIdAsync(teacherDocumentId, cancellationToken);
        if (domainSubmission == null)
            return;

        domainSubmission.VerificationStatus = status;
        domainSubmission.ReviewedByAdminId = reviewedByAdminId;
        domainSubmission.ReviewedAt = reviewedByAdminId.HasValue ? DateTime.UtcNow : domainSubmission.ReviewedAt;
        domainSubmission.RejectionReason = rejectionReason;

        await _domainSubmissionRepository.UpdateAsync(domainSubmission);
        await _domainSubmissionRepository.SaveChangesAsync();
    }

    public async Task RefreshTeacherStatusAfterReviewAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return;

        if (teacher.Status == TeacherStatus.Active)
            return;

        var activeRequired = (await _requirementRepository.GetActiveOrderedAsync(cancellationToken))
            .Where(r => r.IsRequired)
            .ToList();

        if (activeRequired.Count == 0)
        {
            var legacyDocs = await _documentRepository.GetByTeacherIdAsync(teacherId);
            if (legacyDocs.Count == 0)
                return;
            await RefreshLegacyDocumentStatusAsync(teacherId, legacyDocs);
            return;
        }

        var submissions = await _submissionRepository.GetByTeacherIdWithRequirementsAsync(teacherId, cancellationToken);
        var submissionByReqId = submissions.ToDictionary(s => s.RequirementId);

        var missingRequired = activeRequired.Where(r => !submissionByReqId.ContainsKey(r.Id)).ToList();
        if (missingRequired.Count > 0)
        {
            _logger.LogDebug(
                "Teacher {TeacherId} missing {Count} required registration submissions",
                teacherId,
                missingRequired.Count);
            return;
        }

        foreach (var req in activeRequired)
        {
            if (!submissionByReqId.TryGetValue(req.Id, out var sub))
                return;

            if (req.RequirementType == RegistrationRequirementType.File)
            {
                if (req.MaxCount > 1)
                {
                    var fileSubs = submissions.Where(s => s.RequirementId == req.Id).ToList();
                    if (fileSubs.Count < req.MinCount)
                        return;
                    if (fileSubs.Any(s => s.VerificationStatus == DocumentVerificationStatus.Rejected))
                    {
                        await SetStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
                        return;
                    }
                    if (fileSubs.Any(s => s.VerificationStatus == DocumentVerificationStatus.Pending))
                    {
                        await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
                        return;
                    }
                    if (!fileSubs.All(s => s.VerificationStatus == DocumentVerificationStatus.Approved))
                        return;
                }
                else
                {
                    if (sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                    {
                        await SetStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
                        return;
                    }
                    if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                    {
                        await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
                        return;
                    }
                    if (sub.VerificationStatus != DocumentVerificationStatus.Approved)
                        return;
                }
            }
            else
            {
                if (sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                {
                    await SetStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
                    return;
                }
                if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                {
                    await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
                    return;
                }
                if (sub.VerificationStatus != DocumentVerificationStatus.Approved)
                    return;
            }
        }

        await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
    }

    public async Task<bool> CanActivateTeacherAccountAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var (_, isReady) = await EvaluateActivationReadinessAsync(teacherId, cancellationToken);
        return isReady;
    }

    public async Task<(bool Success, string? ErrorMessage)> ActivateTeacherAccountAsync(
        int teacherId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return (false, "Teacher not found");

        if (teacher.Status == TeacherStatus.Active)
            return (false, "Teacher account is already active.");

        if (teacher.Status == TeacherStatus.Blocked)
            return (false, "Blocked teacher accounts cannot be activated.");

        var (blockReason, isReady) = await EvaluateActivationReadinessAsync(teacherId, cancellationToken);
        if (!isReady)
            return (false, blockReason ?? "Teacher is not ready for activation.");

        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.Active);
        await _teacherRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Teacher {TeacherId} account activated by admin {AdminId}",
            teacherId,
            adminId);

        await _lifecycleEmailService.SendAccountActivatedAsync(teacherId, cancellationToken);

        return (true, null);
    }

    private async Task RefreshLegacyDocumentStatusAsync(int teacherId, List<TeacherDocument> documents)
    {
        var hasRejected = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Rejected);
        var hasPending = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Pending);
        var allApproved = documents.All(d => d.VerificationStatus == DocumentVerificationStatus.Approved);

        if (hasRejected)
        {
            await SetStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
            return;
        }

        if (!allApproved)
        {
            if (hasPending)
                await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
            return;
        }

        await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
    }

    private async Task<(string? BlockReason, bool IsReady)> EvaluateActivationReadinessAsync(
        int teacherId,
        CancellationToken cancellationToken)
    {
        var activeRequired = (await _requirementRepository.GetActiveOrderedAsync(cancellationToken))
            .Where(r => r.IsRequired)
            .ToList();

        if (activeRequired.Count == 0)
        {
            var legacyDocs = await _documentRepository.GetByTeacherIdAsync(teacherId);
            if (legacyDocs.Count == 0)
                return ("No registration documents found.", false);

            if (legacyDocs.Any(d => d.VerificationStatus == DocumentVerificationStatus.Rejected))
                return ("One or more documents were rejected.", false);

            if (legacyDocs.Any(d => d.VerificationStatus == DocumentVerificationStatus.Pending))
                return ("One or more documents are still pending admin approval.", false);

            if (!legacyDocs.All(d => d.VerificationStatus == DocumentVerificationStatus.Approved))
                return ("Not all documents are approved.", false);
        }
        else
        {
            var submissions = await _submissionRepository.GetByTeacherIdWithRequirementsAsync(teacherId, cancellationToken);
            var submissionByReqId = submissions.ToDictionary(s => s.RequirementId);

            foreach (var req in activeRequired)
            {
                if (!submissionByReqId.TryGetValue(req.Id, out var sub))
                    return ($"Required registration item '{req.Code}' has not been submitted.", false);

                if (req.RequirementType == RegistrationRequirementType.File && req.MaxCount > 1)
                {
                    var fileSubs = submissions.Where(s => s.RequirementId == req.Id).ToList();
                    if (fileSubs.Count < req.MinCount)
                        return ($"Requirement '{req.Code}' requires at least {req.MinCount} file(s).", false);
                    if (fileSubs.Any(s => s.VerificationStatus == DocumentVerificationStatus.Rejected))
                        return ("One or more required registration items were rejected.", false);
                    if (fileSubs.Any(s => s.VerificationStatus == DocumentVerificationStatus.Pending))
                        return ("One or more required registration items are still pending approval.", false);
                    if (!fileSubs.All(s => s.VerificationStatus == DocumentVerificationStatus.Approved))
                        return ("Not all required registration items are approved.", false);
                }
                else
                {
                    if (sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                        return ("One or more required registration items were rejected.", false);
                    if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                        return ("One or more required registration items are still pending approval.", false);
                    if (sub.VerificationStatus != DocumentVerificationStatus.Approved)
                        return ("Not all required registration items are approved.", false);
                }
            }
        }

        var subjectBlock = await GetSubjectActivationBlockReasonAsync(teacherId);
        if (subjectBlock != null)
            return (subjectBlock, false);

        var domainBlock = await GetDomainQuestionActivationBlockReasonAsync(teacherId, cancellationToken);
        if (domainBlock != null)
            return (domainBlock, false);

        return (null, true);
    }

    private async Task<string?> GetDomainQuestionActivationBlockReasonAsync(int teacherId, CancellationToken cancellationToken)
    {
        var domainIds = await _teacherSubjectRepository.GetDistinctDomainIdsForTeacherAsync(teacherId, cancellationToken);
        if (domainIds.Count == 0)
            return null;

        var questions = await _domainQuestionRepository.GetActiveByDomainIdsAsync(domainIds, cancellationToken);
        var required = questions.Where(q => q.IsRequired).ToList();
        if (required.Count == 0)
            return null;

        var submissions = await _domainSubmissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var submissionByQuestionId = submissions.ToDictionary(s => s.QuestionId);

        foreach (var req in required)
        {
            if (!submissionByQuestionId.TryGetValue(req.Id, out var sub))
                return $"Required domain question '{req.Code}' has not been submitted.";

            if (req.RequiresAdminReview)
            {
                if (sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                    return "One or more domain question answers were rejected.";
                if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                    return "One or more domain question answers are still pending approval.";
                if (sub.VerificationStatus != DocumentVerificationStatus.Approved)
                    return "Not all required domain question answers are approved.";
            }
        }

        return null;
    }

    private async Task<string?> GetSubjectActivationBlockReasonAsync(int teacherId)
    {
        var snapshot = await _teacherSubjectRepository.GetSubjectActivationSnapshotAsync(teacherId);
        if (snapshot.Total == 0)
            return "Teacher has not added any teaching subjects yet.";
        if (snapshot.Pending > 0)
            return "One or more subjects are still pending admin approval.";
        if (snapshot.Rejected > 0)
            return "One or more subjects were rejected.";
        if (snapshot.Approved != snapshot.Total)
            return "Not all teaching subjects are approved.";
        return null;
    }

    private async Task SetStatusAsync(int teacherId, TeacherStatus status)
    {
        await _teacherRepository.UpdateStatusAsync(teacherId, status);
        await _teacherRepository.SaveChangesAsync();
    }
}
