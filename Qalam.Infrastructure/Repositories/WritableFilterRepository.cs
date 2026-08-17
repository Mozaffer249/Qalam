using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class WritableFilterRepository : GenericRepositoryAsync<WritableFilterValue>, IWritableFilterRepository
{
    public WritableFilterRepository(ApplicationDBContext context) : base(context) { }

    public async Task<List<WritableFilterSlot>> GetActiveSlotsByDomainIdAsync(int domainId, CancellationToken ct = default)
    {
        return await _dbContext.WritableFilterSlots
            .AsNoTracking()
            .Where(s => s.DomainId == domainId && s.IsActive)
            .OrderBy(s =>
                (s.Code != null && s.Code.EndsWith(".other")) ||
                s.NameAr == "أخرى" ||
                s.NameAr.Contains("أخرى") ||
                s.NameEn == "Other" ||
                s.NameEn.StartsWith("Other ")
                    ? 1
                    : 0)
            .ThenBy(s => s.OrderIndex)
            .ToListAsync(ct);
    }

    public async Task<List<FilterOptionDto>> GetValuesAsOptionsAsync(
        int slotId,
        string? subjectCode = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.WritableFilterValues
            .AsNoTracking()
            .Where(v => v.SlotId == slotId && v.IsActive);

        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            query = query.Where(v =>
                string.IsNullOrEmpty(v.SubjectCodeContains)
                || subjectCode.Contains(v.SubjectCodeContains));
        }
        else
        {
            // No subject yet: only unscoped values (skill/purpose stay shared).
            query = query.Where(v => string.IsNullOrEmpty(v.SubjectCodeContains));
        }

        return await query
            .OrderByDescending(v => v.IsSeeded)
            .ThenBy(v =>
                (v.Code != null && v.Code.EndsWith(".other")) ||
                v.NameAr == "أخرى" ||
                v.NameAr.Contains("أخرى") ||
                v.NameEn == "Other" ||
                v.NameEn.StartsWith("Other ")
                    ? 1
                    : 0)
            .ThenBy(v => v.NameEn)
            .Select(v => new FilterOptionDto
            {
                Id = v.Id,
                NameAr = v.NameAr,
                NameEn = v.NameEn,
                Code = v.Code
            })
            .ToListAsync(ct);
    }

    public Task<WritableFilterSlot?> GetSlotByDomainAndCodeAsync(int domainId, string slotCode, CancellationToken ct = default)
    {
        return _dbContext.WritableFilterSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.DomainId == domainId && s.Code == slotCode && s.IsActive, ct);
    }

    public Task<WritableFilterValue?> GetByIdWithSlotAsync(int id, CancellationToken ct = default)
    {
        return _dbContext.WritableFilterValues
            .Include(v => v.Slot)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public Task<WritableFilterValue?> FindByNormalizedAsync(int slotId, string normalizedText, CancellationToken ct = default)
    {
        return _dbContext.WritableFilterValues
            .FirstOrDefaultAsync(v => v.SlotId == slotId && v.NormalizedText == normalizedText, ct);
    }

    public async Task<List<WritableFilterValue>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        return await _dbContext.WritableFilterValues
            .AsNoTracking()
            .Include(v => v.Slot)
            .Where(v => ids.Contains(v.Id) && v.IsActive)
            .ToListAsync(ct);
    }
}
