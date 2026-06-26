using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherDomainQuestionRepository
    : GenericRepositoryAsync<TeacherDomainQuestion>, ITeacherDomainQuestionRepository
{
    private readonly DbSet<TeacherDomainQuestion> _set;

    public TeacherDomainQuestionRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherDomainQuestion>();
    }

    public Task<List<TeacherDomainQuestion>> GetAllOrderedAsync(int? domainId = null, CancellationToken cancellationToken = default)
    {
        var q = _set.AsNoTracking().Include(x => x.Domain).AsQueryable();
        if (domainId.HasValue)
            q = q.Where(x => x.DomainId == domainId.Value);

        return q.OrderBy(x => x.DomainId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TeacherDomainQuestion>> GetActiveByDomainIdsAsync(
        IReadOnlyCollection<int> domainIds,
        CancellationToken cancellationToken = default)
    {
        if (domainIds.Count == 0)
            return Task.FromResult(new List<TeacherDomainQuestion>());

        return _set.AsNoTracking()
            .Where(q => domainIds.Contains(q.DomainId) && q.IsActive)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<TeacherDomainQuestion>> GetActiveByDomainIdAsync(int domainId, CancellationToken cancellationToken = default) =>
        GetActiveByDomainIdsAsync(new[] { domainId }, cancellationToken);

    public Task<TeacherDomainQuestion?> GetByIdWithDomainAsync(int id, CancellationToken cancellationToken = default) =>
        _set.Include(x => x.Domain).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> CodeExistsInDomainAsync(int domainId, string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var q = _set.AsNoTracking().Where(x => x.DomainId == domainId && x.Code == code);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);
        return q.AnyAsync(cancellationToken);
    }

    public Task<List<int>> GetDomainIdsWithActiveRequiredQuestionsAsync(CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(q => q.IsActive && q.IsRequired)
            .Select(q => q.DomainId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

    public Task<bool> HasSubmissionsAsync(int questionId, CancellationToken cancellationToken = default) =>
        _dbContext.Set<TeacherDomainQuestionSubmission>()
            .AnyAsync(s => s.QuestionId == questionId, cancellationToken);
}
