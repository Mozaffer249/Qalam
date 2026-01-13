using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
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

    public async Task<EducationDomain> CreateDomainAsync(EducationDomain domain)
    {
        if (!await IsDomainCodeUniqueAsync(domain.Code))
            throw new InvalidOperationException("Domain code already exists");

        domain.CreatedAt = DateTime.UtcNow;
        return await _domainRepository.AddAsync(domain);
    }

    public async Task<EducationDomain> UpdateDomainAsync(EducationDomain domain)
    {
        var existing = await _domainRepository.GetByIdAsync(domain.Id);
        if (existing == null)
            throw new InvalidOperationException("Domain not found");

        if (!await IsDomainCodeUniqueAsync(domain.Code, domain.Id))
            throw new InvalidOperationException("Domain code already exists");

        existing.NameAr = domain.NameAr;
        existing.NameEn = domain.NameEn;
        existing.Code = domain.Code;
        existing.DescriptionAr = domain.DescriptionAr;
        existing.DescriptionEn = domain.DescriptionEn;
        existing.HasCurriculum = domain.HasCurriculum;
        existing.IsActive = domain.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _domainRepository.UpdateAsync(existing);
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
