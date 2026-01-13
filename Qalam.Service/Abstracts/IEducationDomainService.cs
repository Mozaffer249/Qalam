using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface IEducationDomainService
{
    // Query operations
    IQueryable<EducationDomain> GetDomainsQueryable();
    IQueryable<EducationDomain> GetActiveDomainsQueryable();
    Task<EducationDomain> GetDomainByIdAsync(int id);
    Task<EducationDomainDto?> GetDomainDtoByIdAsync(int id);
    Task<EducationDomain> GetDomainWithLevelsAsync(int id);
    Task<EducationDomain> GetDomainByCodeAsync(string code);
    Task<PaginatedResult<EducationDomainDto>> GetPaginatedDomainsAsync(int pageNumber, int pageSize, string? search = null);

    // Command operations
    Task<EducationDomain> CreateDomainAsync(EducationDomain domain);
    Task<EducationDomain> UpdateDomainAsync(EducationDomain domain);
    Task<bool> DeleteDomainAsync(int id);
    Task<bool> ToggleDomainStatusAsync(int id);

    // Validation
    Task<bool> IsDomainCodeUniqueAsync(string code, int? excludeId = null);
}
