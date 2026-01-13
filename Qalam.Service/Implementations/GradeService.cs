using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class GradeService : IGradeService
{
    private readonly IEducationLevelRepository _levelRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly IAcademicTermRepository _termRepository;

    public GradeService(
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        IAcademicTermRepository termRepository)
    {
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
        _termRepository = termRepository;
    }

    #region Education Level Operations

    public IQueryable<EducationLevel> GetLevelsQueryable()
    {
        return _levelRepository.GetLevelsQueryable();
    }

    public IQueryable<EducationLevel> GetLevelsByDomainIdQueryable(int domainId)
    {
        return _levelRepository.GetLevelsByDomainId(domainId);
    }

    public IQueryable<EducationLevel> GetLevelsByCurriculumIdQueryable(int curriculumId)
    {
        return _levelRepository.GetLevelsByCurriculumId(curriculumId);
    }

    public async Task<EducationLevel> GetLevelByIdAsync(int id)
    {
        return await _levelRepository.GetByIdAsync(id);
    }

    public async Task<EducationLevelDto?> GetLevelDtoByIdAsync(int id)
    {
        return await _levelRepository.GetLevelDtoByIdAsync(id);
    }

    public async Task<EducationLevel> GetLevelWithGradesAsync(int id)
    {
        return await _levelRepository.GetLevelWithGradesAsync(id);
    }

    public async Task<EducationLevel> CreateLevelAsync(EducationLevel level)
    {
        if (!await IsLevelCodeUniqueAsync(level.NameEn))
            throw new InvalidOperationException("Level name already exists");

        level.CreatedAt = DateTime.UtcNow;
        return await _levelRepository.AddAsync(level);
    }

    public async Task<EducationLevel> UpdateLevelAsync(EducationLevel level)
    {
        var existing = await _levelRepository.GetByIdAsync(level.Id);
        if (existing == null)
            throw new InvalidOperationException("Level not found");

        if (!await IsLevelCodeUniqueAsync(level.NameEn, level.Id))
            throw new InvalidOperationException("Level name already exists");

        existing.NameAr = level.NameAr;
        existing.NameEn = level.NameEn;
        existing.DomainId = level.DomainId;
        existing.CurriculumId = level.CurriculumId;
        existing.OrderIndex = level.OrderIndex;
        existing.IsActive = level.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _levelRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteLevelAsync(int id)
    {
        var level = await _levelRepository.GetLevelWithGradesAsync(id);
        if (level == null)
            return false;

        if (level.Grades?.Any() == true)
            throw new InvalidOperationException("Cannot delete level with existing grades");

        await _levelRepository.DeleteAsync(level);
        return true;
    }

    #endregion

    #region Grade Operations

    public IQueryable<Grade> GetGradesQueryable()
    {
        return _gradeRepository.GetGradesQueryable();
    }

    public IQueryable<Grade> GetGradesByLevelIdQueryable(int levelId)
    {
        return _gradeRepository.GetGradesByLevelId(levelId);
    }

    public IQueryable<Grade> GetGradesByCurriculumIdQueryable(int curriculumId)
    {
        return _gradeRepository.GetGradesByCurriculumId(curriculumId);
    }

    public async Task<Grade> GetGradeByIdAsync(int id)
    {
        return await _gradeRepository.GetByIdAsync(id);
    }

    public async Task<GradeDto?> GetGradeDtoByIdAsync(int id)
    {
        return await _gradeRepository.GetGradeDtoByIdAsync(id);
    }

    public async Task<Grade> GetGradeWithSubjectsAsync(int id)
    {
        return await _gradeRepository.GetGradeWithSubjectsAsync(id);
    }

    public async Task<Grade> CreateGradeAsync(Grade grade)
    {
        if (!await IsGradeCodeUniqueAsync(grade.NameEn))
            throw new InvalidOperationException("Grade name already exists");

        grade.CreatedAt = DateTime.UtcNow;
        return await _gradeRepository.AddAsync(grade);
    }

    public async Task<Grade> UpdateGradeAsync(Grade grade)
    {
        var existing = await _gradeRepository.GetByIdAsync(grade.Id);
        if (existing == null)
            throw new InvalidOperationException("Grade not found");

        if (!await IsGradeCodeUniqueAsync(grade.NameEn, grade.Id))
            throw new InvalidOperationException("Grade name already exists");

        existing.NameAr = grade.NameAr;
        existing.NameEn = grade.NameEn;
        existing.LevelId = grade.LevelId;
        existing.OrderIndex = grade.OrderIndex;
        existing.IsActive = grade.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _gradeRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteGradeAsync(int id)
    {
        var grade = await _gradeRepository.GetGradeWithSubjectsAsync(id);
        if (grade == null)
            return false;

        if (grade.Subjects?.Any() == true)
            throw new InvalidOperationException("Cannot delete grade with existing subjects");

        await _gradeRepository.DeleteAsync(grade);
        return true;
    }

    #endregion

    #region Academic Term Operations

    public IQueryable<AcademicTerm> GetTermsQueryable()
    {
        return _termRepository.GetTermsQueryable();
    }

    public IQueryable<AcademicTerm> GetTermsByCurriculumIdQueryable(int curriculumId)
    {
        return _termRepository.GetTermsByCurriculumId(curriculumId);
    }

    public async Task<AcademicTerm> GetTermByIdAsync(int id)
    {
        return await _termRepository.GetByIdAsync(id);
    }

    public async Task<AcademicTermDto?> GetTermDtoByIdAsync(int id)
    {
        return await _termRepository.GetTermDtoByIdAsync(id);
    }

    public async Task<AcademicTerm> GetCurrentTermAsync(int curriculumId)
    {
        return await _termRepository.GetCurrentTermAsync(curriculumId);
    }

    public async Task<AcademicTerm> CreateTermAsync(AcademicTerm term)
    {
        term.CreatedAt = DateTime.UtcNow;
        return await _termRepository.AddAsync(term);
    }

    public async Task<AcademicTerm> UpdateTermAsync(AcademicTerm term)
    {
        var existing = await _termRepository.GetByIdAsync(term.Id);
        if (existing == null)
            throw new InvalidOperationException("Term not found");

        existing.NameAr = term.NameAr;
        existing.NameEn = term.NameEn;
        existing.CurriculumId = term.CurriculumId;
        existing.OrderIndex = term.OrderIndex;
        existing.IsMandatory = term.IsMandatory;
        existing.IsActive = term.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _termRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteTermAsync(int id)
    {
        var term = await _termRepository.GetByIdAsync(id);
        if (term == null)
            return false;

        await _termRepository.DeleteAsync(term);
        return true;
    }

    #endregion

    #region Pagination

    public async Task<PaginatedResult<EducationLevelDto>> GetPaginatedLevelsAsync(
        int pageNumber, int pageSize, int? domainId = null, int? curriculumId = null, string? search = null)
    {
        var query = _levelRepository.GetLevelsDtoQueryable();

        if (domainId.HasValue)
            query = query.Where(l => l.DomainId == domainId.Value);

        if (curriculumId.HasValue)
            query = query.Where(l => l.CurriculumId == curriculumId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l =>
                l.NameAr.Contains(search) ||
                l.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(l => l.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<EducationLevelDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<GradeDto>> GetPaginatedGradesAsync(
        int pageNumber, int pageSize, int? levelId = null, int? curriculumId = null, string? search = null)
    {
        var query = _gradeRepository.GetGradesDtoQueryable();

        if (levelId.HasValue)
            query = query.Where(g => g.LevelId == levelId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(g =>
                g.NameAr.Contains(search) ||
                g.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(g => g.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<GradeDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<AcademicTermDto>> GetPaginatedTermsAsync(
        int pageNumber, int pageSize, int? curriculumId = null)
    {
        var query = _termRepository.GetTermsDtoQueryable();

        if (curriculumId.HasValue)
            query = query.Where(t => t.CurriculumId == curriculumId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(t => t.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<AcademicTermDto>(items, totalCount, pageNumber, pageSize);
    }

    #endregion

    #region Validation

    public async Task<bool> IsLevelCodeUniqueAsync(string code, int? excludeId = null)
    {
        return await _levelRepository.IsLevelCodeUniqueAsync(code, excludeId);
    }

    public async Task<bool> IsGradeCodeUniqueAsync(string code, int? excludeId = null)
    {
        return await _gradeRepository.IsGradeCodeUniqueAsync(code, excludeId);
    }

    #endregion
}
