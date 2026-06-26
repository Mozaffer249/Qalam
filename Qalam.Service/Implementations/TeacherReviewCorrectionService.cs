using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherReviewCorrectionService : ITeacherReviewCorrectionService
{
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherDomainQuestionSubmissionRepository _domainSubmissionRepository;

    public TeacherReviewCorrectionService(
        ITeacherDocumentRepository documentRepository,
        ITeacherDomainQuestionSubmissionRepository domainSubmissionRepository)
    {
        _documentRepository = documentRepository;
        _domainSubmissionRepository = domainSubmissionRepository;
    }

    public async Task<List<TeacherReviewCorrectionDto>> GetPendingCorrectionsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var corrections = new List<TeacherReviewCorrectionDto>();

        var rejectedDocs = await _documentRepository.GetRejectedDocumentsAsync(teacherId);
        corrections.AddRange(rejectedDocs.Select(d => new TeacherReviewCorrectionDto
        {
            Type = TeacherReviewCorrectionType.RegistrationDocument,
            DocumentId = d.DocumentId,
            Label = d.DocumentType.ToString(),
            RejectionReason = d.RejectionReason
        }));

        var domainSubmissions = await _domainSubmissionRepository.GetByTeacherIdWithQuestionsAsync(teacherId, cancellationToken);
        var rejectedDomain = domainSubmissions
            .Where(s => s.VerificationStatus == DocumentVerificationStatus.Rejected)
            .ToList();

        corrections.AddRange(rejectedDomain.Select(s => new TeacherReviewCorrectionDto
        {
            Type = TeacherReviewCorrectionType.DomainQuestion,
            DomainId = s.Question.DomainId,
            DomainCode = s.Question.Domain?.Code,
            SubmissionId = s.Id,
            Label = s.Question.NameEn,
            RejectionReason = s.RejectionReason ?? string.Empty
        }));

        return corrections;
    }
}
