using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherDomainApprovalRepository
    : GenericRepositoryAsync<TeacherDomainApproval>, ITeacherDomainApprovalRepository
{
    private readonly DbSet<TeacherDomainApproval> _set;

    public TeacherDomainApprovalRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherDomainApproval>();
    }

    public Task<List<TeacherDomainApproval>> GetByTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .ToListAsync(cancellationToken);

    public Task<TeacherDomainApproval?> GetByTeacherAndDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(a => a.TeacherId == teacherId && a.DomainId == domainId, cancellationToken);

    public Task<bool> HasActiveApprovalAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .AnyAsync(a => a.TeacherId == teacherId && a.RevokedAt == null, cancellationToken);

    public Task<bool> IsDomainApprovedAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .AnyAsync(
                a => a.TeacherId == teacherId && a.DomainId == domainId && a.RevokedAt == null,
                cancellationToken);

    public Task<List<TeacherDomainApproval>> GetActiveByTeacherIdsAsync(
        IReadOnlyList<int> teacherIds,
        CancellationToken cancellationToken = default)
    {
        if (teacherIds.Count == 0)
            return Task.FromResult(new List<TeacherDomainApproval>());

        return _set.AsNoTracking()
            .Where(a => teacherIds.Contains(a.TeacherId) && a.RevokedAt == null)
            .ToListAsync(cancellationToken);
    }
}
