using Microsoft.EntityFrameworkCore;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EducationDeleteGuardService : IEducationDeleteGuardService
{
    private readonly ICurriculumRepository _curriculumRepository;
    private readonly IEducationLevelRepository _levelRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IAcademicTermRepository _termRepository;
    private readonly IContentUnitRepository _contentUnitRepository;

    public EducationDeleteGuardService(
        ICurriculumRepository curriculumRepository,
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        ISubjectRepository subjectRepository,
        IAcademicTermRepository termRepository,
        IContentUnitRepository contentUnitRepository)
    {
        _curriculumRepository = curriculumRepository;
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
        _subjectRepository = subjectRepository;
        _termRepository = termRepository;
        _contentUnitRepository = contentUnitRepository;
    }

    public async Task AssertCanDeleteCurriculumAsync(int id)
    {
        var hasLevels = await _curriculumRepository
            .GetCurriculumWithLevelsAsync(id);
        if (hasLevels?.EducationLevels?.Any() == true)
            throw new InvalidOperationException("Cannot delete curriculum with existing education levels");
    }

    public async Task AssertCanDeleteLevelAsync(int id)
    {
        var level = await _levelRepository.GetLevelWithGradesAsync(id);
        if (level?.Grades?.Any() == true)
            throw new InvalidOperationException("Cannot delete level with existing grades");
    }

    public async Task AssertCanDeleteGradeAsync(int id)
    {
        var grade = await _gradeRepository.GetGradeWithSubjectsAsync(id);
        if (grade?.Subjects?.Any() == true)
            throw new InvalidOperationException("Cannot delete grade with existing subjects");
    }

    public async Task AssertCanDeleteSubjectAsync(int id)
    {
        var subject = await _subjectRepository.GetSubjectWithDetailsAsync(id);
        if (subject?.ContentUnits?.Any() == true)
            throw new InvalidOperationException("Cannot delete subject with existing content units");
    }

    public async Task AssertCanDeleteTermAsync(int id)
    {
        var hasSubjects = await _subjectRepository
            .GetSubjectsByTermId(id)
            .AnyAsync();
        if (hasSubjects)
            throw new InvalidOperationException("Cannot delete term with existing subjects");
    }

    public async Task AssertCanDeleteContentUnitAsync(int id)
    {
        var unit = await _contentUnitRepository.GetContentUnitWithLessonsAsync(id);
        if (unit?.Lessons?.Any() == true)
            throw new InvalidOperationException("Cannot delete content unit with existing lessons");
    }
}
