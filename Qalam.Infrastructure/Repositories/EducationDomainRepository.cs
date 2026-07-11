using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
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
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
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
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                EducationRule = d.EducationRule == null ? null : new EducationRuleDto
                {
                    Id = d.EducationRule.Id,
                    DomainId = d.EducationRule.DomainId,
                    HasCurriculum = d.EducationRule.HasCurriculum,
                    HasEducationLevel = d.EducationRule.HasEducationLevel,
                    HasGrade = d.EducationRule.HasGrade,
                    HasAcademicTerm = d.EducationRule.HasAcademicTerm,
                    HasContentUnits = d.EducationRule.HasContentUnits,
                    HasLessons = d.EducationRule.HasLessons,
                    RequiresQuranContentType = d.EducationRule.RequiresQuranContentType,
                    RequiresQuranLevel = d.EducationRule.RequiresQuranLevel,
                    RequiresUnitTypeSelection = d.EducationRule.RequiresUnitTypeSelection,
                    MinSessions = d.EducationRule.MinSessions,
                    MaxSessions = d.EducationRule.MaxSessions,
                    DefaultSessionDurationMinutes = d.EducationRule.DefaultSessionDurationMinutes,
                    AllowExtension = d.EducationRule.AllowExtension,
                    AllowFlexibleCourses = d.EducationRule.AllowFlexibleCourses,
                    MinGroupSize = d.EducationRule.MinGroupSize,
                    MaxGroupSize = d.EducationRule.MaxGroupSize,
                    NotesAr = d.EducationRule.NotesAr,
                    NotesEn = d.EducationRule.NotesEn,
                    RulesConfigured = d.EducationRule.RulesConfigured,
                }
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EducationDomain> GetDomainWithLevelsAsync(int id)
    {
        return await _dbContext.EducationDomains
            .Include(d => d.EducationLevels)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<EducationDomain> GetDomainWithDetailsAsync(int id)
    {
        return await _dbContext.EducationDomains
            .Include(d => d.EducationLevels)
            .Include(d => d.EducationRule)
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

    public async Task<EducationRule?> GetEducationRuleByDomainCodeAsync(string domainCode)
    {
        return await _dbContext.EducationDomains
            .AsNoTracking()
            .Where(d => d.Code == domainCode)
            .Select(d => d.EducationRule)
            .FirstOrDefaultAsync();
    }
    public async Task<EducationRule?> GetEducationRuleByDomainIdAsync(int domainId)
    {
        return await _dbContext.EducationDomains
            .AsNoTracking()
            .Where(d => d.Id == domainId)
            .Select(d => d.EducationRule)
            .FirstOrDefaultAsync();
    }
}
