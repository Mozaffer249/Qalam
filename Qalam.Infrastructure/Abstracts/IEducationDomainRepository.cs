using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEducationDomainRepository : IGenericRepositoryAsync<EducationDomain>
{
    IQueryable<EducationDomain> GetDomainsQueryable();
    IQueryable<EducationDomain> GetActiveDomainsQueryable();
    Task<EducationDomain> GetDomainWithLevelsAsync(int id);
    Task<EducationDomain> GetDomainByCodeAsync(string code);
    Task<bool> IsDomainCodeUniqueAsync(string code, int? excludeId = null);
}
