using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class SubjectRepository : GenericRepositoryAsync<Subject>, ISubjectRepository
{
    public SubjectRepository(ApplicationDBContext context) : base(context) { }

    public IQueryable<Subject> GetSubjectsQueryable()
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .AsQueryable();
    }

    public IQueryable<SubjectDto> GetSubjectsDtoQueryable()
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .Select(s => new SubjectDto
            {
                Id = s.Id,

                LevelId = s.LevelId,

                GradeId = s.GradeId,

                TermId = s.TermId,
                ParentSubjectId = s.ParentSubjectId,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                DescriptionAr = s.DescriptionAr,
                DescriptionEn = s.DescriptionEn,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            });
    }

    public async Task<SubjectDto?> GetSubjectDtoByIdAsync(int id)
    {
        return await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                DomainId = s.DomainId,
                CurriculumId = s.CurriculumId,
                LevelId = s.LevelId,
                GradeId = s.GradeId,
                TermId = s.TermId,
                ParentSubjectId = s.ParentSubjectId,
                Code = s.Code,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                DescriptionAr = s.DescriptionAr,
                DescriptionEn = s.DescriptionEn,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public IQueryable<Subject> GetSubjectsByDomainId(int domainId)
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.DomainId == domainId)
            .Include(s => s.Grade)
            .Include(s => s.Level)
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByGradeId(int gradeId)
    {
        return OrderSubjectsOtherLast(
                _dbContext.Subjects
                    .AsNoTracking()
                    .Where(s => s.GradeId == gradeId)
                    .Include(s => s.Domain))
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByLevelId(int levelId)
    {
        return OrderSubjectsOtherLast(
                _dbContext.Subjects
                    .AsNoTracking()
                    .Where(s => s.LevelId == levelId)
                    .Include(s => s.Domain)
                    .Include(s => s.Grade))
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByCurriculumId(int curriculumId)
    {
        return OrderSubjectsOtherLast(
                _dbContext.Subjects
                    .AsNoTracking()
                    .Where(s => s.CurriculumId == curriculumId)
                    .Include(s => s.Domain)
                    .Include(s => s.Grade)
                    .Include(s => s.Level))
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByTermId(int termId)
    {
        return OrderSubjectsOtherLast(
                _dbContext.Subjects
                    .AsNoTracking()
                    .Where(s => s.TermId == termId)
                    .Include(s => s.Domain)
                    .Include(s => s.Grade)
                    .Include(s => s.Level))
            .AsQueryable();
    }

    public IQueryable<Subject> GetActiveSubjectsQueryable()
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Domain)
            .Include(s => s.Grade)
            .AsQueryable();
    }

    public async Task<Subject?> GetSubjectWithDetailsAsync(int id)
    {
        return await _dbContext.Subjects
            .Include(s => s.Domain)
            .Include(s => s.Grade)
            .Include(s => s.Level)
            .Include(s => s.Curriculum)
            .Include(s => s.ContentUnits.Where(cu => cu.IsActive))
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Subject>> GetSubjectsWithContentUnitsAsync(int gradeId)
    {
        return await OrderSubjectsOtherLast(
                _dbContext.Subjects
                    .AsNoTracking()
                    .Where(s => s.GradeId == gradeId && s.IsActive)
                    .Include(s => s.ContentUnits.Where(cu => cu.IsActive)))
            .ToListAsync();
    }

    public async Task<List<FilterOptionDto>> GetSubjectsAsOptionsAsync(
        int domainId,
        int? curriculumId,
        int? levelId,
        int? gradeId,
        int? termId,
        int? academicProgramId = null,
        int? parentSubjectId = null,
        bool parentsOnly = false)
    {
        var query = _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.DomainId == domainId && s.IsActive);

        if (parentsOnly)
            query = query.Where(s => s.ParentSubjectId == null);
        else if (parentSubjectId.HasValue)
            query = query.Where(s => s.ParentSubjectId == parentSubjectId);
        else
            query = query.Where(s => s.ParentSubjectId == null);

        // University path: subjects are owned by the program. Do not also require LevelId —
        // seed/data may attach subjects to a single year while the wizard still picks Level.
        if (academicProgramId.HasValue)
        {
            query = query.Where(s => s.AcademicProgramId == academicProgramId);
        }
        else
        {
            if (curriculumId.HasValue)
                query = query.Where(s => s.CurriculumId == curriculumId);

            if (levelId.HasValue)
                query = query.Where(s => s.LevelId == null || s.LevelId == levelId);

            if (gradeId.HasValue)
                query = query.Where(s => s.GradeId == null || s.GradeId == gradeId);

            if (termId.HasValue)
                query = query.Where(s => s.TermId == termId);
        }

        return await OrderSubjectsOtherLast(query)
            .Select(s => new FilterOptionDto
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                Code = s.Code,
                CanDelete = !s.ContentUnits.Any()
            })
            .ToListAsync();
    }

    /// <summary>
    /// Put «أخرى» / Other / *.other last, then alphabetical English name.
    /// Expression is inlined so EF can translate it to SQL.
    /// </summary>
    private static IQueryable<Subject> OrderSubjectsOtherLast(IQueryable<Subject> query) =>
        query
            .OrderBy(s =>
                (s.Code != null && s.Code.EndsWith(".other")) ||
                s.NameAr == "أخرى" ||
                s.NameAr.Contains("أخرى") ||
                s.NameEn == "Other" ||
                s.NameEn.StartsWith("Other ")
                    ? 1
                    : 0)
            .ThenBy(s => s.NameEn);
}
