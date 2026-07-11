using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EducationDomainService : IEducationDomainService
{
    private readonly IEducationDomainRepository _domainRepository;

    public EducationDomainService(IEducationDomainRepository domainRepository)
    {
        _domainRepository = domainRepository;
    }

    #region Query Operations

    public IQueryable<EducationDomain> GetDomainsQueryable()
    {
        return _domainRepository.GetDomainsQueryable();
    }

    public IQueryable<EducationDomain> GetActiveDomainsQueryable()
    {
        return _domainRepository.GetActiveDomainsQueryable();
    }

    public async Task<EducationDomain> GetDomainByIdAsync(int id)
    {
        return await _domainRepository.GetByIdAsync(id);
    }

    public async Task<EducationDomainDto?> GetDomainDtoByIdAsync(int id)
    {
        return await _domainRepository.GetDomainDtoByIdAsync(id);
    }

    public async Task<EducationDomain> GetDomainWithLevelsAsync(int id)
    {
        return await _domainRepository.GetDomainWithLevelsAsync(id);
    }

    public async Task<EducationDomain> GetDomainWithDetailsAsync(int id)
    {
        return await _domainRepository.GetDomainWithDetailsAsync(id);
    }

    public async Task<EducationDomain> GetDomainByCodeAsync(string code)
    {
        return await _domainRepository.GetDomainByCodeAsync(code);
    }

    public async Task<PaginatedResult<EducationDomainDto>> GetPaginatedDomainsAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        var query = _domainRepository.GetDomainsDtoQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d =>
                d.NameAr.Contains(search) ||
                d.NameEn.Contains(search) ||
                d.Code.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.NameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<EducationDomainDto>(items, totalCount, pageNumber, pageSize);
    }

    #endregion

    #region Command Operations

    public async Task<EducationDomain> CreateDomainAsync(EducationDomain domain, EducationRuleDto? educationRule = null)
    {
        if (!await IsDomainCodeUniqueAsync(domain.Code))
            throw new InvalidOperationException($"Code '{domain.Code}' already exists");

        var ruleDto = educationRule ?? EducationRuleDefaults.ForDomainCode(domain.Code);
        domain.CreatedAt = DateTime.UtcNow;
        domain.EducationRule = EducationRuleDefaults.MapToEntity(ruleDto, 0);
        domain.EducationRule.RulesConfigured = educationRule != null;

        return await _domainRepository.AddAsync(domain);
    }

    public async Task<EducationDomain> UpdateDomainAsync(EducationDomain domain, EducationRuleDto? educationRule = null)
    {
        var existing = await _domainRepository.GetDomainWithDetailsAsync(domain.Id);
        if (existing == null)
            throw new InvalidOperationException("Domain not found");

        if (!await IsDomainCodeUniqueAsync(domain.Code, domain.Id))
            throw new InvalidOperationException($"Code '{domain.Code}' already exists");

        existing.NameAr = domain.NameAr;
        existing.NameEn = domain.NameEn;
        existing.Code = domain.Code;
        existing.DescriptionAr = domain.DescriptionAr;
        existing.DescriptionEn = domain.DescriptionEn;
        existing.IsActive = domain.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        if (educationRule != null)
        {
            if (existing.EducationRule == null)
                existing.EducationRule = EducationRuleDefaults.MapToEntity(educationRule, existing.Id);
            else
                EducationRuleDefaults.MapToEntity(educationRule, existing.Id, existing.EducationRule);

            existing.EducationRule.RulesConfigured = true;
        }

        await _domainRepository.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteDomainAsync(int id)
    {
        var domain = await _domainRepository.GetDomainWithLevelsAsync(id);
        if (domain == null)
            return false;

        if (domain.EducationLevels?.Any() == true)
            throw new InvalidOperationException("Cannot delete domain with existing education levels");

        await _domainRepository.DeleteAsync(domain);
        return true;
    }

    public async Task<bool> ToggleDomainStatusAsync(int id)
    {
        var domain = await _domainRepository.GetByIdAsync(id);
        if (domain == null)
            return false;

        domain.IsActive = !domain.IsActive;
        domain.UpdatedAt = DateTime.UtcNow;
        await _domainRepository.UpdateAsync(domain);
        return true;
    }

    #endregion

    #region Validation

    public async Task<bool> IsDomainCodeUniqueAsync(string code, int? excludeId = null)
    {
        return await _domainRepository.IsDomainCodeUniqueAsync(code, excludeId);
    }

    #endregion
}
