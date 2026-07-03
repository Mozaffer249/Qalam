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
    private readonly ITeacherDomainQuestionRepository _domainQuestionRepository;
    private readonly ITeacherDomainQuestionSubmissionRepository _domainSubmissionRepository;
    private readonly ITeacherLifecycleEmailService _lifecycleEmailService;
    private readonly ILogger<TeacherRegistrationCompletionService> _logger;

    public TeacherRegistrationCompletionService(
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRepository teacherRepository,
        ITeacherDomainQuestionRepository domainQuestionRepository,
        ITeacherDomainQuestionSubmissionRepository domainSubmissionRepository,
        ITeacherLifecycleEmailService lifecycleEmailService,
        ILogger<TeacherRegistrationCompletionService> logger)
    {
        _requirementRepository = requirementRepository;
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _teacherRepository = teacherRepository;
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
        var submissionsByReqId = GroupSubmissionsByRequirementId(submissions);

        var missingRequired = activeRequired.Where(r => IsRequirementMissing(r, submissionsByReqId)).ToList();
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
            if (!submissionsByReqId.TryGetValue(req.Id, out var reqSubs))
                return;

            if (req.RequirementType == RegistrationRequirementType.File)
            {
                if (req.MaxCount > 1)
                {
                    var fileSubs = reqSubs;
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
                    var sub = GetRepresentativeSubmission(reqSubs);
                    if (sub == null)
                        return;

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
                var sub = GetRepresentativeSubmission(reqSubs);
                if (sub == null)
                    return;

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

        if (await HasRejectedDomainQuestionSubmissionsAsync(teacherId, cancellationToken))
        {
            await SetStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
            return;
        }

        await SetStatusAsync(teacherId, TeacherStatus.PendingVerification);
    }

    private async Task<bool> HasRejectedDomainQuestionSubmissionsAsync(
        int teacherId,
        CancellationToken cancellationToken)
    {
        var submissions = await _domainSubmissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var latestByQuestionId = BuildLatestSubmissionByQuestionId(submissions);
        return latestByQuestionId.Values.Any(s => s.VerificationStatus == DocumentVerificationStatus.Rejected);
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

        if (teacher.Status == TeacherStatus.DocumentsRejected)
            return (false, "Rejected documents or domain answers must be corrected before activation.");

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

        if (await HasRejectedDomainQuestionSubmissionsAsync(teacherId, CancellationToken.None))
        {
            await SetStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
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
            var submissionsByReqId = GroupSubmissionsByRequirementId(submissions);

            foreach (var req in activeRequired)
            {
                if (IsRequirementMissing(req, submissionsByReqId))
                    return ($"Required registration item '{req.Code}' has not been submitted.", false);

                if (!submissionsByReqId.TryGetValue(req.Id, out var reqSubs))
                    return ($"Required registration item '{req.Code}' has not been submitted.", false);

                if (req.RequirementType == RegistrationRequirementType.File && req.MaxCount > 1)
                {
                    var fileSubs = reqSubs;
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
                    var sub = GetRepresentativeSubmission(reqSubs);
                    if (sub == null)
                        return ($"Required registration item '{req.Code}' has not been submitted.", false);

                    if (sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                        return ("One or more required registration items were rejected.", false);
                    if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                        return ("One or more required registration items are still pending approval.", false);
                    if (sub.VerificationStatus != DocumentVerificationStatus.Approved)
                        return ("Not all required registration items are approved.", false);
                }
            }
        }

        var domainBlock = await GetDomainQuestionActivationBlockReasonAsync(teacherId, cancellationToken);
        if (domainBlock != null)
            return (domainBlock, false);

        return (null, true);
    }

    private async Task<string?> GetDomainQuestionActivationBlockReasonAsync(int teacherId, CancellationToken cancellationToken)
    {
        var submissions = await _domainSubmissionRepository.GetByTeacherIdWithQuestionsAsync(teacherId, cancellationToken);
        var latestByQuestionId = BuildLatestSubmissionByQuestionId(submissions);

        if (latestByQuestionId.Values.Any(s => s.VerificationStatus == DocumentVerificationStatus.Rejected))
            return "One or more domain verification answers were rejected.";

        if (latestByQuestionId.Values.Any(s =>
                s.Question.RequiresAdminReview
                && s.VerificationStatus == DocumentVerificationStatus.Pending))
            return "One or more domain verification answers are still pending admin review.";

        var domainIds = await _domainQuestionRepository.GetDomainIdsWithActiveRequiredQuestionsAsync(cancellationToken);
        if (domainIds.Count == 0)
            return null;

        var questions = await _domainQuestionRepository.GetActiveByDomainIdsAsync(domainIds, cancellationToken);
        var requiredByDomain = questions.Where(q => q.IsRequired).GroupBy(q => q.DomainId).ToList();
        if (requiredByDomain.Count == 0)
            return null;

        foreach (var domain in requiredByDomain)
        {
            var allApproved = domain.All(q =>
                latestByQuestionId.TryGetValue(q.Id, out var sub)
                && sub.VerificationStatus == DocumentVerificationStatus.Approved);
            if (allApproved)
                return null;
        }

        return "No education domain has all its required questions approved yet.";
    }

    public async Task<bool> HasPendingRequiredRegistrationReviewAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var activeRequired = (await _requirementRepository.GetActiveOrderedAsync(cancellationToken))
            .Where(r => r.IsRequired)
            .ToList();

        if (activeRequired.Count == 0)
        {
            var legacyDocs = await _documentRepository.GetByTeacherIdAsync(teacherId);
            return legacyDocs.Any(d => d.VerificationStatus == DocumentVerificationStatus.Pending);
        }

        var submissions = await _submissionRepository.GetByTeacherIdWithRequirementsAsync(teacherId, cancellationToken);
        var submissionsByReqId = GroupSubmissionsByRequirementId(submissions);

        foreach (var req in activeRequired)
        {
            if (IsRequirementMissing(req, submissionsByReqId))
                return true;

            if (!submissionsByReqId.TryGetValue(req.Id, out var reqSubs))
                return true;

            if (req.RequirementType == RegistrationRequirementType.File && req.MaxCount > 1)
            {
                if (reqSubs.Count < req.MinCount)
                    return true;
                if (reqSubs.Any(s => s.VerificationStatus == DocumentVerificationStatus.Pending))
                    return true;
            }
            else
            {
                var sub = GetRepresentativeSubmission(reqSubs);
                if (sub?.VerificationStatus == DocumentVerificationStatus.Pending)
                    return true;
            }
        }

        return false;
    }

    private static Dictionary<int, List<TeacherRegistrationSubmission>> GroupSubmissionsByRequirementId(
        IReadOnlyList<TeacherRegistrationSubmission> submissions) =>
        submissions.GroupBy(s => s.RequirementId).ToDictionary(g => g.Key, g => g.ToList());

    private static bool IsRequirementMissing(
        TeacherRegistrationRequirement req,
        IReadOnlyDictionary<int, List<TeacherRegistrationSubmission>> submissionsByReqId)
    {
        if (!submissionsByReqId.TryGetValue(req.Id, out var subs) || subs.Count == 0)
            return true;

        if (req.RequirementType == RegistrationRequirementType.File && req.MaxCount > 1)
            return subs.Count < req.MinCount;

        return false;
    }

    private static TeacherRegistrationSubmission? GetRepresentativeSubmission(
        IReadOnlyList<TeacherRegistrationSubmission> subs) =>
        subs.OrderByDescending(s => (int)s.VerificationStatus).FirstOrDefault();

    private static Dictionary<int, TeacherDomainQuestionSubmission> BuildLatestSubmissionByQuestionId(
        IReadOnlyList<TeacherDomainQuestionSubmission> submissions) =>
        submissions
            .GroupBy(s => s.QuestionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Id).First());

    private async Task SetStatusAsync(int teacherId, TeacherStatus status)
    {
        await _teacherRepository.UpdateStatusAsync(teacherId, status);
        await _teacherRepository.SaveChangesAsync();
    }
}
