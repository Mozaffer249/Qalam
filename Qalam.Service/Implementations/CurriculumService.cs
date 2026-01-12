using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class CurriculumService : ICurriculumService
{
    private readonly ICurriculumRepository _curriculumRepository;

    public CurriculumService(ICurriculumRepository curriculumRepository)
    {
        _curriculumRepository = curriculumRepository;
    }

    #region Query Operations

    public IQueryable<Curriculum> GetCurriculumsQueryable()
    {
        return _curriculumRepository.GetCurriculumsQueryable();
    }

    public IQueryable<Curriculum> GetActiveCurriculumsQueryable()
    {
        return _curriculumRepository.GetActiveCurriculumsQueryable();
    }

    public async Task<Curriculum> GetCurriculumByIdAsync(int id)
    {
        return await _curriculumRepository.GetByIdAsync(id);
    }

    public async Task<Curriculum> GetCurriculumWithLevelsAsync(int id)
    {
        return await _curriculumRepository.GetCurriculumWithLevelsAsync(id);
    }

    public async Task<Curriculum> GetCurriculumByCodeAsync(string code)
    {
        return await _curriculumRepository.GetCurriculumByCodeAsync(code);
    }

    public async Task<PaginatedResult<Curriculum>> GetPaginatedCurriculumsAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        var query = _curriculumRepository.GetCurriculumsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.NameAr.Contains(search) ||
                c.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.NameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Curriculum>(items, totalCount, pageNumber, pageSize);
    }

    #endregion

    #region Command Operations

    public async Task<Curriculum> CreateCurriculumAsync(Curriculum curriculum)
    {
        if (!await IsCurriculumCodeUniqueAsync(curriculum.NameEn))
            throw new InvalidOperationException("Curriculum name already exists");

        curriculum.CreatedAt = DateTime.UtcNow;
        return await _curriculumRepository.AddAsync(curriculum);
    }

    public async Task<Curriculum> UpdateCurriculumAsync(Curriculum curriculum)
    {
        var existing = await _curriculumRepository.GetByIdAsync(curriculum.Id);
        if (existing == null)
            throw new InvalidOperationException("Curriculum not found");

        if (!await IsCurriculumCodeUniqueAsync(curriculum.NameEn, curriculum.Id))
            throw new InvalidOperationException("Curriculum name already exists");

        existing.NameAr = curriculum.NameAr;
        existing.NameEn = curriculum.NameEn;
        existing.DescriptionAr = curriculum.DescriptionAr;
        existing.DescriptionEn = curriculum.DescriptionEn;
        existing.IsActive = curriculum.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _curriculumRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteCurriculumAsync(int id)
    {
        var curriculum = await _curriculumRepository.GetCurriculumWithLevelsAsync(id);
        if (curriculum == null)
            return false;

        if (curriculum.EducationLevels?.Any() == true)
            throw new InvalidOperationException("Cannot delete curriculum with existing education levels");

        await _curriculumRepository.DeleteAsync(curriculum);
        return true;
    }

    public async Task<bool> ToggleCurriculumStatusAsync(int id)
    {
        var curriculum = await _curriculumRepository.GetByIdAsync(id);
        if (curriculum == null)
            return false;

        curriculum.IsActive = !curriculum.IsActive;
        curriculum.UpdatedAt = DateTime.UtcNow;
        await _curriculumRepository.UpdateAsync(curriculum);
        return true;
    }

    #endregion

    #region Validation

    public async Task<bool> IsCurriculumCodeUniqueAsync(string code, int? excludeId = null)
    {
        return await _curriculumRepository.IsCurriculumCodeUniqueAsync(code, excludeId);
    }

    #endregion
}
