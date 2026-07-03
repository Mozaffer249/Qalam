using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherDomainSubjectCascadeService : ITeacherDomainSubjectCascadeService
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherDomainQuestionRepository _questionRepository;
    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ILogger<TeacherDomainSubjectCascadeService> _logger;

    public TeacherDomainSubjectCascadeService(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherDomainQuestionRepository questionRepository,
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ILogger<TeacherDomainSubjectCascadeService> logger)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _questionRepository = questionRepository;
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task RejectSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        int adminId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var subjects = await _teacherSubjectRepository.GetSubjectsInDomainForCascadeRejectAsync(
            teacherId, domainId, cancellationToken);

        if (subjects.Count == 0)
            return;

        var cascadeReason = $"Domain verification failed: {reason.Trim()}";

        foreach (var subject in subjects)
        {
            subject.VerificationStatus = DocumentVerificationStatus.Rejected;
            subject.RejectionReason = cascadeReason;
            subject.RejectionSource = TeacherSubjectRejectionSource.DomainQuestionCascade;
            subject.IsActive = false;
            subject.ReviewedByAdminId = adminId;
            subject.ReviewedAt = DateTime.UtcNow;
            subject.UpdatedAt = DateTime.UtcNow;
            subject.UpdatedBy = adminId;

            await _teacherSubjectRepository.UpdateAsync(subject);
        }

        await _teacherSubjectRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Cascade-rejected {Count} subject(s) for teacher {TeacherId} in domain {DomainId}",
            subjects.Count,
            teacherId,
            domainId);
    }

    public async Task ApproveSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsDomainFullyApprovedForTeacherAsync(teacherId, domainId, cancellationToken))
            return;

        var subjects = await _teacherSubjectRepository.GetTeacherSubjectsInDomainAsync(
            teacherId, domainId, cancellationToken);

        if (subjects.Count == 0)
            return;

        foreach (var subject in subjects)
        {
            subject.VerificationStatus = DocumentVerificationStatus.Approved;
            subject.RejectionReason = null;
            subject.RejectionSource = null;
            subject.IsActive = true;
            subject.ReviewedByAdminId = null;
            subject.ReviewedAt = null;
            subject.UpdatedAt = DateTime.UtcNow;

            await _teacherSubjectRepository.UpdateAsync(subject);
        }

        await _teacherSubjectRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Approved {Count} subject(s) in domain {DomainId} for teacher {TeacherId}",
            subjects.Count,
            domainId,
            teacherId);
    }

    public async Task<bool> IsDomainFullyApprovedForTeacherAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default)
    {
        var questions = await _questionRepository.GetActiveByDomainIdAsync(domainId, cancellationToken);
        if (questions.Count == 0)
            return true;

        var submissions = await _submissionRepository.GetByTeacherAndDomainIdAsync(teacherId, domainId, cancellationToken);
        var submissionByQuestionId = BuildLatestSubmissionByQuestionId(submissions);

        foreach (var question in questions)
        {
            if (QuestionBlocksDomainApproval(question, submissionByQuestionId))
                return false;
        }

        return true;
    }

    public async Task<string?> GetSubjectSaveBlockReasonForDomainAsync(
        int teacherId,
        int domainId,
        string domainNameEn,
        string domainCode,
        CancellationToken cancellationToken = default)
    {
        if (await IsDomainFullyApprovedForTeacherAsync(teacherId, domainId, cancellationToken))
            return null;

        var questions = await _questionRepository.GetActiveByDomainIdAsync(domainId, cancellationToken);
        if (questions.Count == 0)
            return null;

        var submissions = await _submissionRepository.GetByTeacherAndDomainIdAsync(teacherId, domainId, cancellationToken);
        var submissionByQuestionId = BuildLatestSubmissionByQuestionId(submissions);

        var required = questions.Where(q => q.IsRequired).ToList();
        if (required.Any(q => !submissionByQuestionId.ContainsKey(q.Id)))
        {
            return $"Complete domain questions for '{domainNameEn}' ({domainCode}) before adding subjects.";
        }

        if (questions.Any(q =>
                submissionByQuestionId.TryGetValue(q.Id, out var sub)
                && sub.VerificationStatus == DocumentVerificationStatus.Rejected))
        {
            return $"Fix rejected domain verification for '{domainNameEn}' ({domainCode}) before adding subjects.";
        }

        return $"Complete and wait for approval of domain requirements for '{domainNameEn}' ({domainCode}) before adding subjects.";
    }

    private static bool QuestionBlocksDomainApproval(
        TeacherDomainQuestion question,
        Dictionary<int, TeacherDomainQuestionSubmission> submissionByQuestionId)
    {
        if (question.IsRequired)
        {
            if (!submissionByQuestionId.TryGetValue(question.Id, out var requiredSub))
                return true;

            return requiredSub.VerificationStatus != DocumentVerificationStatus.Approved;
        }

        if (!question.RequiresAdminReview)
            return false;

        if (!submissionByQuestionId.TryGetValue(question.Id, out var optionalSub))
            return false;

        return optionalSub.VerificationStatus is DocumentVerificationStatus.Pending
            or DocumentVerificationStatus.Rejected;
    }

    private static Dictionary<int, TeacherDomainQuestionSubmission> BuildLatestSubmissionByQuestionId(
        IReadOnlyList<TeacherDomainQuestionSubmission> submissions) =>
        submissions
            .GroupBy(s => s.QuestionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Id).First());
}
