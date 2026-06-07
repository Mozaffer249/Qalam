using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherRegistrationSubmissionRepository
    : GenericRepositoryAsync<TeacherRegistrationSubmission>, ITeacherRegistrationSubmissionRepository
{
    private readonly DbSet<TeacherRegistrationSubmission> _set;

    public TeacherRegistrationSubmissionRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherRegistrationSubmission>();
    }

    public Task<List<TeacherRegistrationSubmission>> GetByTeacherIdWithRequirementsAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _set.Include(s => s.Requirement)
            .Include(s => s.TeacherDocument)
            .Where(s => s.TeacherId == teacherId)
            .ToListAsync(cancellationToken);

    public Task<TeacherRegistrationSubmission?> GetByTeacherAndRequirementAsync(
        int teacherId,
        int requirementId,
        CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(s => s.TeacherId == teacherId && s.RequirementId == requirementId, cancellationToken);

    public Task<List<TeacherRegistrationSubmission>> GetByTeacherAndRequirementCodeAsync(
        int teacherId,
        string requirementCode,
        CancellationToken cancellationToken = default) =>
        _set.Include(s => s.Requirement)
            .Where(s => s.TeacherId == teacherId && s.Requirement.Code == requirementCode)
            .ToListAsync(cancellationToken);

    public Task<TeacherRegistrationSubmission?> GetByTeacherDocumentIdAsync(
        int teacherDocumentId,
        CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(s => s.TeacherDocumentId == teacherDocumentId, cancellationToken);

    public Task<int> DeleteAllForTeacherAsync(int teacherId, CancellationToken cancellationToken = default) =>
        _set.Where(s => s.TeacherId == teacherId).ExecuteDeleteAsync(cancellationToken);
}
