using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class EducationDomainRepository : GenericRepositoryAsync<EducationDomain>, IEducationDomainRepository
{
    public EducationDomainRepository(ApplicationDBContext context) : base(context) { }

    public IQueryable<EducationDomain> GetDomainsQueryable()
    {
        return _dbContext.EducationDomains
            .AsNoTracking()
            .AsQueryable();
    }

    public IQueryable<EducationDomain> GetActiveDomainsQueryable()
    {
        return _dbContext.EducationDomains
            .AsNoTracking()
            .Where(d => d.IsActive)
            .AsQueryable();
    }

    public async Task<EducationDomain> GetDomainWithLevelsAsync(int id)
    {
        return await _dbContext.EducationDomains
            .Include(d => d.EducationLevels)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<EducationDomain> GetDomainByCodeAsync(string code)
    {
        return await _dbContext.EducationDomains
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code);
    }

    public async Task<bool> IsDomainCodeUniqueAsync(string code, int? excludeId = null)
    {
        return !await _dbContext.EducationDomains
            .AnyAsync(d => d.Code == code && d.Id != excludeId);
    }
}
