using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherLevelRepository : GenericRepositoryAsync<TeacherLevel>, ITeacherLevelRepository
{
    private readonly DbSet<TeacherLevel> _set;

    public TeacherLevelRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherLevel>();
    }

    public Task<TeacherLevel?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Code == code, cancellationToken);

    public Task<TeacherLevel?> GetStarterLevelAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<TeacherLevel>> ListOrderedAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(cancellationToken);

    public Task<TeacherLevel?> GetNextLevelAsync(int currentOrderIndex, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(l => l.IsActive && l.OrderIndex > currentOrderIndex)
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync(cancellationToken);
}
