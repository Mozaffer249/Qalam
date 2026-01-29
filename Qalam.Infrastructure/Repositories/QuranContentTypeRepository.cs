using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class QuranContentTypeRepository : GenericRepositoryAsync<QuranContentType>, IQuranContentTypeRepository
{
    private readonly ApplicationDBContext _context;

    public QuranContentTypeRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<QuranContentType> GetContentTypesQueryable()
    {
        return _context.QuranContentTypes
            .AsNoTracking()
            .OrderBy(ct => ct.NameEn);
    }

    public IQueryable<QuranContentType> GetActiveContentTypesQueryable()
    {
        return _context.QuranContentTypes
            .AsNoTracking()
            .Where(ct => ct.IsActive)
            .OrderBy(ct => ct.NameEn);
    }

    public async Task<QuranContentType> GetContentTypeByCodeAsync(string code)
    {
        return await _context.QuranContentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Code == code);
    }

    public async Task<List<FilterOptionDto>> GetQuranContentTypesAsOptionsAsync()
    {
        return await _context.QuranContentTypes
            .AsNoTracking()
            .Where(ct => ct.IsActive)
            .OrderBy(ct => ct.NameEn)
            .Select(ct => new FilterOptionDto
            {
                Id = ct.Id,
                NameAr = ct.NameAr,
                NameEn = ct.NameEn,
                Code = ct.Code
            })
            .ToListAsync();
    }
}
