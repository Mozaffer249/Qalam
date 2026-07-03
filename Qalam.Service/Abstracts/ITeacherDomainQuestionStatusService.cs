using Qalam.Data.DTOs;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherDomainQuestionStatusService
{
    Task<List<EducationDomainTeacherDto>> EnrichDomainsForTeacherAsync(
        IReadOnlyList<EducationDomainDto> domains,
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<bool> DomainRequiresAnswerAsync(int teacherId, int domainId, CancellationToken cancellationToken = default);

    Task<string?> ValidateSubjectsDomainQuestionsAsync(
        int teacherId,
        IEnumerable<int> subjectIds,
        CancellationToken cancellationToken = default);

    Task<List<TeacherDomainQuestionGroupDto>> GetChecklistForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherDomainQuestionStatusResponseDto> GetDomainQuestionStatusAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<bool> HasRejectedDomainQuestionsAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<List<TeacherReviewCorrectionDto>> GetRejectedDomainCorrectionsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyDomainRequiringAnswerAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<List<int>> GetCatalogDomainIdsWithRequiredQuestionsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasIncompleteCatalogDomainAnswersAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<bool> AreAllCatalogDomainsFullyApprovedAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<bool> HasAnyFullyApprovedCatalogDomainAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<bool> HasCatalogDomainsPendingAdminReviewAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<bool> HasAnyAnswersPendingAdminReviewAsync(int teacherId, CancellationToken cancellationToken = default);
}
