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
}
