using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class DepartmentRepository : GenericRepositoryAsync<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDBContext context) : base(context) { }

    public async Task<List<DepartmentDto>> GetDepartmentsDtoAsync(int? collegeId = null, bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _dbContext.Departments.AsNoTracking().AsQueryable();
        if (collegeId.HasValue) q = q.Where(d => d.CollegeId == collegeId.Value);
        if (activeOnly) q = q.Where(d => d.IsActive);
        return await q
            .OrderBy(d => d.NameEn)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                CollegeId = d.CollegeId,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                Code = d.Code,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public Task<DepartmentDto?> GetDepartmentDtoByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Departments.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                CollegeId = d.CollegeId,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                Code = d.Code,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        _dbContext.Departments.AsNoTracking().AnyAsync(d => d.Id == id, ct);

    public async Task<List<FilterOptionDto>> GetDepartmentsAsOptionsAsync(int collegeId, CancellationToken ct = default) =>
        await _dbContext.Departments.AsNoTracking()
            .Where(d => d.CollegeId == collegeId && d.IsActive)
            .OrderBy(d => d.NameEn)
            .Select(d => new FilterOptionDto
            {
                Id = d.Id,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                Code = d.Code,
            })
            .ToListAsync(ct);
}
