using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class UniversityRepository : GenericRepositoryAsync<University>, IUniversityRepository
{
    public UniversityRepository(ApplicationDBContext context) : base(context) { }

    public async Task<List<UniversityDto>> GetUniversitiesDtoAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _dbContext.Universities.AsNoTracking().AsQueryable();
        if (activeOnly) q = q.Where(u => u.IsActive);
        return await q
            .OrderBy(u => u.NameEn)
            .Select(u => new UniversityDto
            {
                Id = u.Id,
                NameAr = u.NameAr,
                NameEn = u.NameEn,
                Code = u.Code,
                Country = u.Country,
                City = u.City,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public Task<UniversityDto?> GetUniversityDtoByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Universities.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UniversityDto
            {
                Id = u.Id,
                NameAr = u.NameAr,
                NameEn = u.NameEn,
                Code = u.Code,
                Country = u.Country,
                City = u.City,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        _dbContext.Universities.AsNoTracking().AnyAsync(u => u.Id == id, ct);

    public async Task<List<FilterOptionDto>> GetUniversitiesAsOptionsAsync(CancellationToken ct = default) =>
        await _dbContext.Universities.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.NameEn)
            .Select(u => new FilterOptionDto
            {
                Id = u.Id,
                NameAr = u.NameAr,
                NameEn = u.NameEn,
                Code = u.Code,
            })
            .ToListAsync(ct);
}
