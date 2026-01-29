using Qalam.Data.DTOs;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IQuranContentTypeRepository : IGenericRepositoryAsync<QuranContentType>
{
    IQueryable<QuranContentType> GetContentTypesQueryable();
    IQueryable<QuranContentType> GetActiveContentTypesQueryable();
    Task<QuranContentType> GetContentTypeByCodeAsync(string code);

    // Filter options
    Task<List<FilterOptionDto>> GetQuranContentTypesAsOptionsAsync();
}
