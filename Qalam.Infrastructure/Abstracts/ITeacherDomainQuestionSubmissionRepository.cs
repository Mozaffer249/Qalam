using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDomainQuestionSubmissionRepository : IGenericRepositoryAsync<TeacherDomainQuestionSubmission>
{
    Task<List<TeacherDomainQuestionSubmission>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<List<TeacherDomainQuestionSubmission>> GetByTeacherIdWithQuestionsAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<TeacherDomainQuestionSubmission?> GetByIdWithQuestionAsync(int submissionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForTeacherAndQuestionAsync(int teacherId, int questionId, CancellationToken cancellationToken = default);
    Task<TeacherDomainQuestionSubmission?> GetByTeacherDocumentIdAsync(int teacherDocumentId, CancellationToken cancellationToken = default);
}
