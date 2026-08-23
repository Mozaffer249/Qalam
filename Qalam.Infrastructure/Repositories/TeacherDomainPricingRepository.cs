using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherDomainPricingRepository : GenericRepositoryAsync<TeacherDomainPricing>, ITeacherDomainPricingRepository
{
    private readonly DbSet<TeacherDomainPricing> _set;

    public TeacherDomainPricingRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherDomainPricing>();
    }

    public Task<TeacherDomainPricing?> GetByTeacherAndDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _set.Include(p => p.TeacherLevel)
            .Include(p => p.Domain)
            .Include(p => p.Teacher).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(p => p.TeacherId == teacherId && p.DomainId == domainId, cancellationToken);

    public Task<List<TeacherDomainPricing>> ListByTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(p => p.TeacherLevel)
            .Include(p => p.Domain)
            .Where(p => p.TeacherId == teacherId)
            .OrderBy(p => p.DomainId)
            .ToListAsync(cancellationToken);

    public async Task<TeacherDomainPricing> GetOrCreateAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _set
            .Include(p => p.TeacherLevel)
            .FirstOrDefaultAsync(p => p.TeacherId == teacherId && p.DomainId == domainId, cancellationToken);
        if (existing != null)
            return existing;

        var row = new TeacherDomainPricing
        {
            TeacherId = teacherId,
            DomainId = domainId,
            ReflectCustomIndividualPriceToStudent = false,
            ReflectCustomGroupPriceToStudent = false,
            HasCompletedInterviewSession = false,
            CreatedAt = DateTime.UtcNow
        };
        await _set.AddAsync(row, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return row;
    }
}
