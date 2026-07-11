using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class LessonRepository : GenericRepositoryAsync<Lesson>, ILessonRepository
{
    private readonly ApplicationDBContext _context;

    public LessonRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<Lesson> GetLessonsQueryable()
    {
        return _context.Lessons
            .AsNoTracking()
            .Include(l => l.Unit)
            .OrderBy(l => l.Unit.OrderIndex)
            .ThenBy(l => l.OrderIndex);
    }

    public IQueryable<Lesson> GetLessonsByContentUnitId(int contentUnitId, int? quranContentTypeId = null, int? quranLevelId = null)
    {
        var query = _context.Lessons
            .AsNoTracking()
            .Where(l => l.UnitId == contentUnitId);

        if (quranContentTypeId.HasValue && quranLevelId.HasValue)
        {
            query = query.Where(l =>
                l.QuranContentTypeId == quranContentTypeId.Value &&
                l.QuranLevelId == quranLevelId.Value);
        }
        else
        {
            query = query.Where(l => l.QuranContentTypeId == null && l.QuranLevelId == null);
        }

        return query.OrderBy(l => l.OrderIndex);
    }

    public IQueryable<Lesson> GetLessonsBySubjectId(int subjectId)
    {
        return _context.Lessons
            .AsNoTracking()
            .Include(l => l.Unit)
            .Where(l => l.Unit.SubjectId == subjectId)
            .OrderBy(l => l.Unit.OrderIndex)
            .ThenBy(l => l.OrderIndex);
    }

    public async Task<Lesson> GetLessonWithDetailsAsync(int id)
    {
        return await _context.Lessons
            .Include(l => l.Unit)
            .ThenInclude(u => u.Subject)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<int> GetNextOrderIndexAsync(int contentUnitId, int? quranContentTypeId = null, int? quranLevelId = null)
    {
        var query = _context.Lessons.Where(l => l.UnitId == contentUnitId);

        if (quranContentTypeId.HasValue && quranLevelId.HasValue)
        {
            query = query.Where(l =>
                l.QuranContentTypeId == quranContentTypeId.Value &&
                l.QuranLevelId == quranLevelId.Value);
        }
        else
        {
            query = query.Where(l => l.QuranContentTypeId == null && l.QuranLevelId == null);
        }

        var maxOrder = await query.MaxAsync(l => (int?)l.OrderIndex) ?? 0;
        return maxOrder + 1;
    }

    public async Task UpdateRangeAsync(IEnumerable<Lesson> entities)
    {
        _context.Lessons.UpdateRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FilterOptionDto>> GetLessonsAsOptionsAsync(int contentUnitId, int? quranContentTypeId = null, int? quranLevelId = null)
    {
        return await GetLessonsByContentUnitId(contentUnitId, quranContentTypeId, quranLevelId)
            .Where(l => l.IsActive)
            .Select(l => new FilterOptionDto
            {
                Id = l.Id,
                NameAr = l.NameAr,
                NameEn = l.NameEn,
                CanDelete = true
            })
            .ToListAsync();
    }
}
