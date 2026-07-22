using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class AcademicProgramRepository : GenericRepositoryAsync<AcademicProgram>, IAcademicProgramRepository
{
    public AcademicProgramRepository(ApplicationDBContext context) : base(context) { }

    public async Task<List<AcademicProgramDto>> GetProgramsDtoAsync(int? departmentId = null, bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _dbContext.AcademicPrograms.AsNoTracking().AsQueryable();
        if (departmentId.HasValue) q = q.Where(p => p.DepartmentId == departmentId.Value);
        if (activeOnly) q = q.Where(p => p.IsActive);
        return await q
            .OrderBy(p => p.NameEn)
            .Select(p => new AcademicProgramDto
            {
                Id = p.Id,
                DepartmentId = p.DepartmentId,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Code = p.Code,
                DegreeType = p.DegreeType,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
            })
            .ToListAsync(ct);
    }

    public Task<AcademicProgramDto?> GetProgramDtoByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new AcademicProgramDto
            {
                Id = p.Id,
                DepartmentId = p.DepartmentId,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Code = p.Code,
                DegreeType = p.DegreeType,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AsNoTracking().AnyAsync(p => p.Id == id, ct);

    public async Task<List<FilterOptionDto>> GetProgramsAsOptionsAsync(int departmentId, CancellationToken ct = default) =>
        await _dbContext.AcademicPrograms.AsNoTracking()
            .Where(p => p.DepartmentId == departmentId && p.IsActive)
            .OrderBy(p => p.NameEn)
            .Select(p => new FilterOptionDto
            {
                Id = p.Id,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Code = p.Code,
            })
            .ToListAsync(ct);
}
