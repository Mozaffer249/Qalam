using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Content;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class ContentManagementService : IContentManagementService
{
    private readonly IContentUnitRepository _contentUnitRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IEducationDeleteGuardService _deleteGuard;

    public ContentManagementService(
        IContentUnitRepository contentUnitRepository,
        ILessonRepository lessonRepository,
        IEducationDeleteGuardService deleteGuard)
    {
        _contentUnitRepository = contentUnitRepository;
        _lessonRepository = lessonRepository;
        _deleteGuard = deleteGuard;
    }

    #region Content Unit Operations

    public IQueryable<ContentUnit> GetContentUnitsQueryable()
    {
        return _contentUnitRepository.GetContentUnitsQueryable();
    }

    public IQueryable<ContentUnit> GetContentUnitsBySubjectIdQueryable(int subjectId)
    {
        return _contentUnitRepository.GetContentUnitsBySubjectId(subjectId);
    }

    public IQueryable<ContentUnit> GetContentUnitsByTermIdQueryable(int termId)
    {
        return _contentUnitRepository.GetContentUnitsByTermId(termId);
    }

    public async Task<ContentUnit> GetContentUnitByIdAsync(int id)
    {
        return await _contentUnitRepository.GetByIdAsync(id);
    }

    public async Task<ContentUnit> GetContentUnitWithLessonsAsync(int id)
    {
        return await _contentUnitRepository.GetContentUnitWithLessonsAsync(id);
    }

    public async Task<ContentUnit> CreateContentUnitAsync(ContentUnit contentUnit)
    {
        contentUnit.OrderIndex = await _contentUnitRepository.GetNextOrderIndexAsync(
            contentUnit.SubjectId, 0);
        contentUnit.CreatedAt = DateTime.UtcNow;
        contentUnit.IsActive = true;
        return await _contentUnitRepository.AddAsync(contentUnit);
    }

    public async Task<ContentUnit> UpdateContentUnitAsync(ContentUnit contentUnit)
    {
        var existing = await _contentUnitRepository.GetByIdAsync(contentUnit.Id);
        if (existing == null)
            throw new InvalidOperationException("Content unit not found");

        existing.NameAr = contentUnit.NameAr;
        existing.NameEn = contentUnit.NameEn;
        existing.SubjectId = contentUnit.SubjectId;
        existing.OrderIndex = contentUnit.OrderIndex;
        existing.IsActive = contentUnit.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _contentUnitRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteContentUnitAsync(int id)
    {
        var contentUnit = await _contentUnitRepository.GetContentUnitWithLessonsAsync(id);
        if (contentUnit == null)
            return false;

        await _deleteGuard.AssertCanDeleteContentUnitAsync(id);

        await _contentUnitRepository.DeleteAsync(contentUnit);
        return true;
    }

    public async Task<bool> ToggleContentUnitStatusAsync(int id)
    {
        var contentUnit = await _contentUnitRepository.GetByIdAsync(id);
        if (contentUnit == null)
            return false;

        contentUnit.IsActive = !contentUnit.IsActive;
        contentUnit.UpdatedAt = DateTime.UtcNow;
        await _contentUnitRepository.UpdateAsync(contentUnit);
        return true;
    }

    public async Task<bool> ReorderContentUnitsAsync(int subjectId, int termId, List<int> orderedIds)
    {
        var contentUnits = await _contentUnitRepository
            .GetContentUnitsBySubjectId(subjectId)
            .ToListAsync();

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var unit = contentUnits.FirstOrDefault(cu => cu.Id == orderedIds[i]);
            if (unit != null)
            {
                unit.OrderIndex = i + 1;
                unit.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _contentUnitRepository.UpdateRangeAsync(contentUnits);
        return true;
    }

    #endregion

    #region Lesson Operations

    public IQueryable<Lesson> GetLessonsQueryable()
    {
        return _lessonRepository.GetLessonsQueryable();
    }

    public IQueryable<Lesson> GetLessonsByContentUnitIdQueryable(int contentUnitId)
    {
        return _lessonRepository.GetLessonsByContentUnitId(contentUnitId);
    }

    public IQueryable<Lesson> GetLessonsBySubjectIdQueryable(int subjectId)
    {
        return _lessonRepository.GetLessonsBySubjectId(subjectId);
    }

    public async Task<Lesson> GetLessonByIdAsync(int id)
    {
        return await _lessonRepository.GetByIdAsync(id);
    }

    public async Task<Lesson> GetLessonWithDetailsAsync(int id)
    {
        return await _lessonRepository.GetLessonWithDetailsAsync(id);
    }

    public async Task<Lesson> CreateLessonAsync(Lesson lesson)
    {
        await ValidateLessonQuranFieldsAsync(lesson);

        if (lesson.OrderIndex <= 0)
        {
            lesson.OrderIndex = await _lessonRepository.GetNextOrderIndexAsync(
                lesson.UnitId,
                lesson.QuranContentTypeId,
                lesson.QuranLevelId);
        }

        lesson.CreatedAt = DateTime.UtcNow;
        lesson.IsActive = true;
        var created = await _lessonRepository.AddAsync(lesson);
        created.Unit = null!;
        created.QuranContentType = null;
        created.QuranLevel = null;
        return created;
    }

    public async Task<Lesson> UpdateLessonAsync(Lesson lesson)
    {
        var existing = await _lessonRepository.GetByIdAsync(lesson.Id);
        if (existing == null)
            throw new InvalidOperationException("Lesson not found");

        await ValidateLessonQuranFieldsAsync(lesson);

        existing.NameAr = lesson.NameAr;
        existing.NameEn = lesson.NameEn;
        existing.UnitId = lesson.UnitId;
        existing.OrderIndex = lesson.OrderIndex;
        existing.IsActive = lesson.IsActive;
        existing.QuranContentTypeId = lesson.QuranContentTypeId;
        existing.QuranLevelId = lesson.QuranLevelId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _lessonRepository.UpdateAsync(existing);
        existing.Unit = null!;
        existing.QuranContentType = null;
        existing.QuranLevel = null;
        return existing;
    }

    public async Task<bool> DeleteLessonAsync(int id)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id);
        if (lesson == null)
            return false;

        await _lessonRepository.DeleteAsync(lesson);
        return true;
    }

    public async Task<bool> ToggleLessonStatusAsync(int id)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id);
        if (lesson == null)
            return false;

        lesson.IsActive = !lesson.IsActive;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.UpdateAsync(lesson);
        return true;
    }

    public async Task<bool> ReorderLessonsAsync(int contentUnitId, List<int> orderedIds)
    {
        var lessons = await _lessonRepository
            .GetLessonsByContentUnitId(contentUnitId)
            .ToListAsync();

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == orderedIds[i]);
            if (lesson != null)
            {
                lesson.OrderIndex = i + 1;
                lesson.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _lessonRepository.UpdateRangeAsync(lessons);
        return true;
    }

    #endregion

    #region Pagination

    public async Task<PaginatedResult<ContentUnitListDto>> GetPaginatedContentUnitsAsync(
        int pageNumber, int pageSize, int? subjectId = null, List<int>? termIds = null, string? unitTypeCode = null, string? search = null)
    {
        var query = _contentUnitRepository.GetContentUnitsQueryable();

        if (subjectId.HasValue)
            query = query.Where(cu => cu.SubjectId == subjectId.Value);

        if (termIds != null && termIds.Any())
            query = query.Where(cu => cu.TermId.HasValue && termIds.Contains(cu.TermId.Value));

        if (!string.IsNullOrEmpty(unitTypeCode))
            query = query.Where(cu => cu.UnitTypeCode == unitTypeCode);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(cu =>
                cu.NameAr.Contains(search) ||
                cu.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(cu => cu.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(cu => new ContentUnitListDto
            {
                Id = cu.Id,
                SubjectId = cu.SubjectId,
                TermId = cu.TermId,
                NameAr = cu.NameAr,
                NameEn = cu.NameEn,
                OrderIndex = cu.OrderIndex,
                UnitTypeCode = cu.UnitTypeCode,
                QuranSurahId = cu.QuranSurahId,
                QuranPartId = cu.QuranPartId,
                IsActive = cu.IsActive,
                CreatedAt = cu.CreatedAt,
                UpdatedAt = cu.UpdatedAt,
            })
            .ToListAsync();

        return new PaginatedResult<ContentUnitListDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<Lesson>> GetPaginatedLessonsAsync(
        int pageNumber, int pageSize, int? contentUnitId = null, int? subjectId = null, string? search = null,
        int? quranContentTypeId = null, int? quranLevelId = null)
    {
        var query = _lessonRepository.GetLessonsQueryable();

        if (contentUnitId.HasValue)
            query = query.Where(l => l.UnitId == contentUnitId.Value);

        if (subjectId.HasValue)
            query = query.Where(l => l.Unit.SubjectId == subjectId.Value);

        if (quranContentTypeId.HasValue && quranLevelId.HasValue)
        {
            query = query.Where(l =>
                l.QuranContentTypeId == quranContentTypeId.Value &&
                l.QuranLevelId == quranLevelId.Value);
        }
        else if (contentUnitId.HasValue)
        {
            query = query.Where(l => l.QuranContentTypeId == null && l.QuranLevelId == null);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l =>
                l.NameAr.Contains(search) ||
                l.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(l => l.Unit.OrderIndex)
            .ThenBy(l => l.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Lesson>(items, totalCount, pageNumber, pageSize);
    }

    private static bool IsQuranUnitType(string? unitTypeCode) =>
        unitTypeCode is "QuranSurah" or "QuranPart";

    private async Task ValidateLessonQuranFieldsAsync(Lesson lesson)
    {
        var unit = await _contentUnitRepository.GetByIdAsync(lesson.UnitId);
        if (unit == null)
            throw new InvalidOperationException("Content unit not found");

        if (IsQuranUnitType(unit.UnitTypeCode))
        {
            if (!lesson.QuranContentTypeId.HasValue || !lesson.QuranLevelId.HasValue)
                throw new InvalidOperationException("Quran content type and level are required for Quran unit lessons");
        }
        else if (lesson.QuranContentTypeId.HasValue || lesson.QuranLevelId.HasValue)
        {
            throw new InvalidOperationException("Quran content type and level are only allowed for Quran units");
        }
    }

    #endregion
}
