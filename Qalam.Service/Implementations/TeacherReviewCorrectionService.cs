using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherReviewCorrectionService : ITeacherReviewCorrectionService
{
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherDomainQuestionStatusService _domainQuestionStatusService;

    public TeacherReviewCorrectionService(
        ITeacherDocumentRepository documentRepository,
        ITeacherDomainQuestionStatusService domainQuestionStatusService)
    {
        _documentRepository = documentRepository;
        _domainQuestionStatusService = domainQuestionStatusService;
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

        corrections.AddRange(
            await _domainQuestionStatusService.GetRejectedDomainCorrectionsAsync(teacherId, cancellationToken));

        return corrections;
    }
}
