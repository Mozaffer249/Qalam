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
    private readonly ILogger<TeacherRegistrationCompletionService> _logger;

    public TeacherRegistrationCompletionService(
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRepository teacherRepository,
        ILogger<TeacherRegistrationCompletionService> logger)
    {
        _requirementRepository = requirementRepository;
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _teacherRepository = teacherRepository;
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
        if (submission == null)
            return;

        submission.VerificationStatus = status;
        submission.ReviewedByAdminId = reviewedByAdminId;
        submission.ReviewedAt = reviewedByAdminId.HasValue ? DateTime.UtcNow : submission.ReviewedAt;
        submission.RejectionReason = rejectionReason;

        await _submissionRepository.UpdateAsync(submission);
        await _submissionRepository.SaveChangesAsync();
    }

    public async Task RefreshTeacherStatusAfterReviewAsync(int teacherId, CancellationToken cancellationToken = default)
    {
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
                        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
                        await _teacherRepository.SaveChangesAsync();
                        return;
                    }
                    if (fileSubs.Any(s => s.VerificationStatus == DocumentVerificationStatus.Pending))
                    {
                        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.PendingVerification);
                        await _teacherRepository.SaveChangesAsync();
                        return;
                    }
                    if (!fileSubs.All(s => s.VerificationStatus == DocumentVerificationStatus.Approved))
                        return;
                }
                else
                {
                    if (sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                    {
                        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
                        await _teacherRepository.SaveChangesAsync();
                        return;
                    }
                    if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                    {
                        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.PendingVerification);
                        await _teacherRepository.SaveChangesAsync();
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
                    await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
                    await _teacherRepository.SaveChangesAsync();
                    return;
                }
                if (sub.VerificationStatus == DocumentVerificationStatus.Pending)
                {
                    await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.PendingVerification);
                    await _teacherRepository.SaveChangesAsync();
                    return;
                }
                if (sub.VerificationStatus != DocumentVerificationStatus.Approved)
                    return;
            }
        }

        _logger.LogInformation("All required registration items approved for teacher {TeacherId}, activating", teacherId);
        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.Active);
        await _teacherRepository.SaveChangesAsync();
    }

    private async Task RefreshLegacyDocumentStatusAsync(int teacherId, List<TeacherDocument> documents)
    {
        var hasRejected = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Rejected);
        var hasPending = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Pending);
        var allApproved = documents.All(d => d.VerificationStatus == DocumentVerificationStatus.Approved);

        TeacherStatus newStatus;
        if (hasRejected)
            newStatus = TeacherStatus.DocumentsRejected;
        else if (allApproved)
            newStatus = TeacherStatus.Active;
        else if (hasPending)
            newStatus = TeacherStatus.PendingVerification;
        else
            return;

        await _teacherRepository.UpdateStatusAsync(teacherId, newStatus);
        await _teacherRepository.SaveChangesAsync();
    }
}
