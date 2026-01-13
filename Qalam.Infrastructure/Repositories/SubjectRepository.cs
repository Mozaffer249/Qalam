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
                DomainId = s.DomainId,
                DomainNameAr = s.Domain.NameAr,
                DomainNameEn = s.Domain.NameEn,
                CurriculumId = s.CurriculumId,
                CurriculumNameAr = s.Curriculum != null ? s.Curriculum.NameAr : null,
                CurriculumNameEn = s.Curriculum != null ? s.Curriculum.NameEn : null,
                LevelId = s.LevelId,
                LevelNameAr = s.Level != null ? s.Level.NameAr : null,
                LevelNameEn = s.Level != null ? s.Level.NameEn : null,
                GradeId = s.GradeId,
                GradeNameAr = s.Grade != null ? s.Grade.NameAr : null,
                GradeNameEn = s.Grade != null ? s.Grade.NameEn : null,
                TermId = s.TermId,
                TermNameAr = s.Term != null ? s.Term.NameAr : null,
                TermNameEn = s.Term != null ? s.Term.NameEn : null,
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
                DomainNameAr = s.Domain.NameAr,
                DomainNameEn = s.Domain.NameEn,
                CurriculumId = s.CurriculumId,
                CurriculumNameAr = s.Curriculum != null ? s.Curriculum.NameAr : null,
                CurriculumNameEn = s.Curriculum != null ? s.Curriculum.NameEn : null,
                LevelId = s.LevelId,
                LevelNameAr = s.Level != null ? s.Level.NameAr : null,
                LevelNameEn = s.Level != null ? s.Level.NameEn : null,
                GradeId = s.GradeId,
                GradeNameAr = s.Grade != null ? s.Grade.NameAr : null,
                GradeNameEn = s.Grade != null ? s.Grade.NameEn : null,
                TermId = s.TermId,
                TermNameAr = s.Term != null ? s.Term.NameAr : null,
                TermNameEn = s.Term != null ? s.Term.NameEn : null,
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
        return _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.GradeId == gradeId)
            .Include(s => s.Domain)
            .OrderBy(s => s.NameEn)
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByLevelId(int levelId)
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.LevelId == levelId)
            .Include(s => s.Domain)
            .Include(s => s.Grade)
            .OrderBy(s => s.NameEn)
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByCurriculumId(int curriculumId)
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.CurriculumId == curriculumId)
            .Include(s => s.Domain)
            .Include(s => s.Grade)
            .Include(s => s.Level)
            .OrderBy(s => s.NameEn)
            .AsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByTermId(int termId)
    {
        return _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TermId == termId)
            .Include(s => s.Domain)
            .Include(s => s.Grade)
            .Include(s => s.Level)
            .OrderBy(s => s.NameEn)
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
        return await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.GradeId == gradeId && s.IsActive)
            .Include(s => s.ContentUnits.Where(cu => cu.IsActive))
            .OrderBy(s => s.NameEn)
            .ToListAsync();
    }
}
