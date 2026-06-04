using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherRegistrationSubmissionRepository : IGenericRepositoryAsync<TeacherRegistrationSubmission>
{
    Task<List<TeacherRegistrationSubmission>> GetByTeacherIdWithRequirementsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherRegistrationSubmission?> GetByTeacherAndRequirementAsync(
        int teacherId,
        int requirementId,
        CancellationToken cancellationToken = default);

    Task<List<TeacherRegistrationSubmission>> GetByTeacherAndRequirementCodeAsync(
        int teacherId,
        string requirementCode,
        CancellationToken cancellationToken = default);

    Task<TeacherRegistrationSubmission?> GetByTeacherDocumentIdAsync(
        int teacherDocumentId,
        CancellationToken cancellationToken = default);
}
