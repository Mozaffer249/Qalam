using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherLevelUpgradeSuggestionRepository
    : GenericRepositoryAsync<TeacherLevelUpgradeSuggestion>, ITeacherLevelUpgradeSuggestionRepository
{
    private readonly DbSet<TeacherLevelUpgradeSuggestion> _set;

    public TeacherLevelUpgradeSuggestionRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherLevelUpgradeSuggestion>();
    }

    public Task<TeacherLevelUpgradeSuggestion?> GetPendingForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TeacherId == teacherId && s.Status == TeacherLevelUpgradeSuggestionStatus.Pending,
                cancellationToken);

    public Task<List<TeacherLevelUpgradeSuggestion>> ListByStatusAsync(
        TeacherLevelUpgradeSuggestionStatus status,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(s => s.Teacher).ThenInclude(t => t.User)
            .Include(s => s.CurrentLevel)
            .Include(s => s.SuggestedLevel)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
}
