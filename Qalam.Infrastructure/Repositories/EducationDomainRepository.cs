using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
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

    public IQueryable<EducationDomainDto> GetDomainsDtoQueryable()
    {
        return _dbContext.EducationDomains
            .AsNoTracking()
            .Select(d => new EducationDomainDto
            {
                Id = d.Id,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                Code = d.Code,
                DescriptionAr = d.DescriptionAr,
                DescriptionEn = d.DescriptionEn,
                CreatedAt = d.CreatedAt
            });
    }

    public async Task<EducationDomainDto?> GetDomainDtoByIdAsync(int id)
    {
        return await _dbContext.EducationDomains
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new EducationDomainDto
            {
                Id = d.Id,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                Code = d.Code,
                DescriptionAr = d.DescriptionAr,
                DescriptionEn = d.DescriptionEn,
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync();
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
        var query = _dbContext.EducationDomains
            .Where(d => d.Code == code);

        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);

        return !await query.AnyAsync();
    }
}
