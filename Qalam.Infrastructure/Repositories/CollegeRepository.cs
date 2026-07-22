using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CollegeRepository : GenericRepositoryAsync<College>, ICollegeRepository
{
    public CollegeRepository(ApplicationDBContext context) : base(context) { }

    public async Task<List<CollegeDto>> GetCollegesDtoAsync(int? universityId = null, bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _dbContext.Colleges.AsNoTracking().AsQueryable();
        if (universityId.HasValue) q = q.Where(c => c.UniversityId == universityId.Value);
        if (activeOnly) q = q.Where(c => c.IsActive);
        return await q
            .OrderBy(c => c.NameEn)
            .Select(c => new CollegeDto
            {
                Id = c.Id,
                UniversityId = c.UniversityId,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Code = c.Code,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public Task<CollegeDto?> GetCollegeDtoByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Colleges.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CollegeDto
            {
                Id = c.Id,
                UniversityId = c.UniversityId,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Code = c.Code,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        _dbContext.Colleges.AsNoTracking().AnyAsync(c => c.Id == id, ct);

    public async Task<List<FilterOptionDto>> GetCollegesAsOptionsAsync(int universityId, CancellationToken ct = default) =>
        await _dbContext.Colleges.AsNoTracking()
            .Where(c => c.UniversityId == universityId && c.IsActive)
            .OrderBy(c => c.NameEn)
            .Select(c => new FilterOptionDto
            {
                Id = c.Id,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Code = c.Code,
            })
            .ToListAsync(ct);
}
