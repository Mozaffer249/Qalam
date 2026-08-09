using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDomainApprovalRepository : IGenericRepositoryAsync<TeacherDomainApproval>
{
    Task<List<TeacherDomainApproval>> GetByTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherDomainApproval?> GetByTeacherAndDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveApprovalAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<bool> IsDomainApprovedAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    Task<List<TeacherDomainApproval>> GetActiveByTeacherIdsAsync(
        IReadOnlyList<int> teacherIds,
        CancellationToken cancellationToken = default);
}
