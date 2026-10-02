using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class PolicyVersionRepository : IPolicyVersionRepository
{
    private readonly ApplicationDBContext _context;

    public PolicyVersionRepository(ApplicationDBContext context) => _context = context;

    public Task<PolicyVersion?> GetEffectiveAsync(DateTime asOf, CancellationToken cancellationToken = default)
        => _context.PolicyVersions.AsNoTracking()
            .Where(v => v.Status != PolicyVersionStatus.Draft
                        && v.EffectiveFrom != null
                        && v.EffectiveFrom <= asOf
                        && (v.EffectiveTo == null || v.EffectiveTo > asOf))
            .OrderByDescending(v => v.EffectiveFrom)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PolicyVersion?> GetByIdAsync(int id, bool track = false, CancellationToken cancellationToken = default)
    {
        var q = track ? _context.PolicyVersions : _context.PolicyVersions.AsNoTracking();
        return q.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public Task<PolicyVersion?> GetDraftAsync(bool track = false, CancellationToken cancellationToken = default)
    {
        var q = track ? _context.PolicyVersions : _context.PolicyVersions.AsNoTracking();
        return q.Where(v => v.Status == PolicyVersionStatus.Draft)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<PolicyVersion>> ListOpenPublishedAsync(CancellationToken cancellationToken = default)
        => _context.PolicyVersions
            .Where(v => v.Status == PolicyVersionStatus.Published && v.EffectiveTo == null)
            .OrderByDescending(v => v.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<List<PolicyVersion>> ListAsync(CancellationToken cancellationToken = default)
        => _context.PolicyVersions.AsNoTracking()
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task<int> GetMaxVersionNumberAsync(CancellationToken cancellationToken = default)
        => await _context.PolicyVersions.MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;

    public async Task AddAsync(PolicyVersion version, CancellationToken cancellationToken = default)
        => await _context.PolicyVersions.AddAsync(version, cancellationToken);

    public void Remove(PolicyVersion version) => _context.PolicyVersions.Remove(version);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}

public class PolicyCaseRepository : IPolicyCaseRepository
{
    private readonly ApplicationDBContext _context;

    public PolicyCaseRepository(ApplicationDBContext context) => _context = context;

    public async Task AddAsync(PolicyCase policyCase, CancellationToken cancellationToken = default)
        => await _context.PolicyCases.AddAsync(policyCase, cancellationToken);

    public Task<PolicyCase?> GetByIdAsync(int id, bool track = false, CancellationToken cancellationToken = default)
    {
        var q = track ? _context.PolicyCases : _context.PolicyCases.AsNoTracking();
        return q.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> ExistsForScheduleAsync(PolicyCaseKind kind, int courseScheduleId, CancellationToken cancellationToken = default)
        => _context.PolicyCases.AnyAsync(c => c.Kind == kind && c.CourseScheduleId == courseScheduleId, cancellationToken);

    public Task<List<PolicyCase>> ListByEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default)
        => _context.PolicyCases.AsNoTracking()
            .Where(c => c.EnrollmentId == enrollmentId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public Task<List<PolicyCase>> ListByScheduleAsync(int courseScheduleId, CancellationToken cancellationToken = default)
        => _context.PolicyCases.AsNoTracking()
            .Where(c => c.CourseScheduleId == courseScheduleId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public async Task<(List<PolicyCase> Items, int Total)> ListAsync(
        PolicyCaseKind? kind,
        PolicyCaseStatus? status,
        DateTime? from,
        DateTime? to,
        int? studentId,
        int? teacherId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _context.PolicyCases.AsNoTracking().AsQueryable();
        if (kind.HasValue) q = q.Where(c => c.Kind == kind);
        if (status.HasValue) q = q.Where(c => c.Status == status);
        if (from.HasValue) q = q.Where(c => c.CreatedAt >= from);
        if (to.HasValue) q = q.Where(c => c.CreatedAt <= to);
        if (studentId.HasValue)
            q = q.Where(c => c.Enrollment.Participants.Any(p => p.StudentId == studentId));
        if (teacherId.HasValue)
            q = q.Where(c => c.Enrollment.ApprovedByTeacherId == teacherId);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
