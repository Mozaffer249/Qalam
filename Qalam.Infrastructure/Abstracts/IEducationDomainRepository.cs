using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEducationDomainRepository : IGenericRepositoryAsync<EducationDomain>
{
    IQueryable<EducationDomain> GetDomainsQueryable();
    IQueryable<EducationDomain> GetActiveDomainsQueryable();
    IQueryable<EducationDomainDto> GetDomainsDtoQueryable();
    Task<EducationDomainDto?> GetDomainDtoByIdAsync(int id);
    Task<EducationDomain> GetDomainWithLevelsAsync(int id);
    Task<EducationDomain> GetDomainByCodeAsync(string code);
    Task<bool> IsDomainCodeUniqueAsync(string code, int? excludeId = null);
    Task<EducationRule?> GetEducationRuleByDomainCodeAsync(string domainCode);
    Task<EducationRule?> GetEducationRuleByDomainIdAsync(int domainId);
}
