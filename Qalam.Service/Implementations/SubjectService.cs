using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    #region Query Operations

    public IQueryable<Subject> GetSubjectsQueryable()
    {
        return _subjectRepository.GetSubjectsQueryable();
    }

    public IQueryable<Subject> GetSubjectsByDomainIdQueryable(int domainId)
    {
        return _subjectRepository.GetSubjectsByDomainId(domainId);
    }

    public IQueryable<Subject> GetSubjectsByCurriculumIdQueryable(int curriculumId)
    {
        return _subjectRepository.GetSubjectsByCurriculumId(curriculumId);
    }

    public IQueryable<Subject> GetSubjectsByLevelIdQueryable(int levelId)
    {
        return _subjectRepository.GetSubjectsByLevelId(levelId);
    }

    public IQueryable<Subject> GetSubjectsByGradeIdQueryable(int gradeId)
    {
        return _subjectRepository.GetSubjectsByGradeId(gradeId);
    }

    public IQueryable<Subject> GetSubjectsByTermIdQueryable(int termId)
    {
        return _subjectRepository.GetSubjectsByTermId(termId);
    }

    public async Task<Subject> GetSubjectByIdAsync(int id)
    {
        return await _subjectRepository.GetByIdAsync(id);
    }

    public async Task<SubjectDto?> GetSubjectDtoByIdAsync(int id)
    {
        return await _subjectRepository.GetSubjectDtoByIdAsync(id);
    }

    public async Task<Subject> GetSubjectWithDetailsAsync(int id)
    {
        return await _subjectRepository.GetSubjectWithDetailsAsync(id);
    }

    #endregion

    #region Command Operations

    public async Task<Subject> CreateSubjectAsync(Subject subject)
    {
        subject.CreatedAt = DateTime.UtcNow;
        subject.IsActive = true;
        return await _subjectRepository.AddAsync(subject);
    }

    public async Task<Subject> UpdateSubjectAsync(Subject subject)
    {
        var existing = await _subjectRepository.GetByIdAsync(subject.Id);
        if (existing == null)
            throw new InvalidOperationException("Subject not found");

        existing.NameAr = subject.NameAr;
        existing.NameEn = subject.NameEn;
        existing.DescriptionAr = subject.DescriptionAr;
        existing.DescriptionEn = subject.DescriptionEn;
        existing.DomainId = subject.DomainId;
        existing.CurriculumId = subject.CurriculumId;
        existing.LevelId = subject.LevelId;
        existing.GradeId = subject.GradeId;
        existing.TermId = subject.TermId;
        existing.IsActive = subject.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _subjectRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteSubjectAsync(int id)
    {
        var subject = await _subjectRepository.GetSubjectWithDetailsAsync(id);
        if (subject == null)
            return false;

        if (subject.ContentUnits?.Any() == true)
            throw new InvalidOperationException("Cannot delete subject with existing content units");

        await _subjectRepository.DeleteAsync(subject);
        return true;
    }

    public async Task<bool> ToggleSubjectStatusAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        if (subject == null)
            return false;

        subject.IsActive = !subject.IsActive;
        subject.UpdatedAt = DateTime.UtcNow;
        await _subjectRepository.UpdateAsync(subject);
        return true;
    }

    #endregion

    #region Pagination

    public async Task<PaginatedResult<SubjectDto>> GetPaginatedSubjectsAsync(
        int pageNumber, int pageSize, int? gradeId = null, int? termId = null, string? search = null)
    {
        var query = _subjectRepository.GetSubjectsDtoQueryable();

        // if (domainId.HasValue)
        //     query = query.Where(s => s.DomainId == domainId.Value);

        // if (curriculumId.HasValue)
        //     query = query.Where(s => s.CurriculumId == curriculumId.Value);

        // if (levelId.HasValue)
        //     query = query.Where(s => s.LevelId == levelId.Value);

        if (gradeId.HasValue)
            query = query.Where(s => s.GradeId == gradeId.Value);

        if (termId.HasValue)
            query = query.Where(s => s.TermId == termId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.NameAr.Contains(search) ||
                s.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.NameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<SubjectDto>(items, totalCount, pageNumber, pageSize);
    }

    #endregion

    #region Validation

    public async Task<List<int>> GetInvalidSubjectIdsAsync(IEnumerable<int> subjectIds)
    {
        var requestedIds = subjectIds.Distinct().ToList();
        
        if (!requestedIds.Any())
            return new List<int>();
        
        // Optimized: only select IDs, not full entities
        var existingIds = await _subjectRepository
            .GetSubjectsQueryable()
            .Where(s => requestedIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();
        
        // Return IDs that don't exist
        return requestedIds.Except(existingIds).ToList();
    }

    #endregion
}
