using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherRegistrationRequirementRepository
    : GenericRepositoryAsync<TeacherRegistrationRequirement>, ITeacherRegistrationRequirementRepository
{
    private readonly DbSet<TeacherRegistrationRequirement> _set;

    public TeacherRegistrationRequirementRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherRegistrationRequirement>();
    }

    public Task<List<TeacherRegistrationRequirement>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public Task<List<TeacherRegistrationRequirement>> GetActiveOrderedAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public Task<TeacherRegistrationRequirement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var q = _set.AsNoTracking().Where(r => r.Code == code);
        if (excludeId.HasValue)
            q = q.Where(r => r.Id != excludeId.Value);
        return q.AnyAsync(cancellationToken);
    }

    public Task<bool> HasSubmissionsAsync(int requirementId, CancellationToken cancellationToken = default) =>
        _dbContext.Set<TeacherRegistrationSubmission>()
            .AnyAsync(s => s.RequirementId == requirementId, cancellationToken);
}
