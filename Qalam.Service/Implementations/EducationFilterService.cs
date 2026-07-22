using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EducationFilterService : IEducationFilterService
{
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ICurriculumRepository _curriculumRepository;
    private readonly IEducationLevelRepository _levelRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly IAcademicTermRepository _termRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IContentUnitRepository _contentUnitRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IQuranContentTypeRepository _quranContentTypeRepository;
    private readonly IQuranLevelRepository _quranLevelRepository;
    private readonly IUniversityRepository _universityRepository;
    private readonly ICollegeRepository _collegeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAcademicProgramRepository _academicProgramRepository;

    public EducationFilterService(
        IEducationDomainRepository domainRepository,
        ICurriculumRepository curriculumRepository,
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        IAcademicTermRepository termRepository,
        ISubjectRepository subjectRepository,
        IContentUnitRepository contentUnitRepository,
        ILessonRepository lessonRepository,
        IQuranContentTypeRepository quranContentTypeRepository,
        IQuranLevelRepository quranLevelRepository,
        IUniversityRepository universityRepository,
        ICollegeRepository collegeRepository,
        IDepartmentRepository departmentRepository,
        IAcademicProgramRepository academicProgramRepository)
    {
        _domainRepository = domainRepository;
        _curriculumRepository = curriculumRepository;
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
        _termRepository = termRepository;
        _subjectRepository = subjectRepository;
        _contentUnitRepository = contentUnitRepository;
        _lessonRepository = lessonRepository;
        _quranContentTypeRepository = quranContentTypeRepository;
        _quranLevelRepository = quranLevelRepository;
        _universityRepository = universityRepository;
        _collegeRepository = collegeRepository;
        _departmentRepository = departmentRepository;
        _academicProgramRepository = academicProgramRepository;
    }

    public async Task<FilterOptionsResponseDto> GetFilterOptionsAsync(FilterStateDto state, int pageNumber = 1, int pageSize = 20)
    {
        if (!state.DomainId.HasValue)
            throw new ArgumentException("DomainId is required");

        var domainId = state.DomainId.Value;

        // Load domain rule
        var rule = await _domainRepository.GetEducationRuleByDomainIdAsync(domainId);
        if (rule == null)
            throw new InvalidOperationException($"Domain with ID '{domainId}' not found or has no rules configured");

        var domain = await _domainRepository.GetByIdAsync(domainId);
        if (domain == null)
            throw new InvalidOperationException($"Domain with ID '{domainId}' not found");

        if (!domain.IsActive)
            throw new InvalidOperationException("Education domain is inactive");

        // Set default UnitTypeCode for Quran domain if not specified
        if (domain.Code?.ToLowerInvariant() == "quran" && string.IsNullOrEmpty(state.UnitTypeCode))
        {
            state.UnitTypeCode = "QuranPart";
        }

        var response = new FilterOptionsResponseDto
        {
            CurrentState = state,
            Rule = MapToRuleDto(rule),
            Options = new List<FilterOptionDto>()
        };

        // Determine next step and load options
        var result = await DetermineNextStepAsync(state, rule, domain, pageNumber, pageSize);
        response.NextStep = result.NextStep;
        response.Options = result.Options;
        response.Unit = result.Unit;
        response.TotalCount = result.TotalCount;
        response.PageNumber = result.PageNumber;
        response.PageSize = result.PageSize;
        response.TotalPages = result.TotalPages;
        response.QuranContentTypes = result.QuranContentTypes;
        response.QuranLevels = result.QuranLevels;

        // For Quran domain, expose auto-selected subject for clients that display it
        if (domain.Code?.ToLowerInvariant() == "quran" && state.SubjectId.HasValue)
        {
            var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
                state.DomainId!.Value,
                curriculumId: null,
                levelId: null,
                gradeId: null,
                termId: null);
            response.SelectedSubject = subjects.FirstOrDefault(s => s.Id == state.SubjectId.Value);
            response.CurrentState.SubjectId = state.SubjectId;
        }

        return response;
    }

    private async Task<FilterStepResult> DetermineNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        EducationDomain domain,
        int pageNumber,
        int pageSize)
    {
        var domainId = domain.Id;
        var isQuranDomain = domain.Code?.ToLowerInvariant() == "quran";

        // ========================================
        // QURAN DOMAIN FLOW
        // Subject → QuranContentType → QuranLevel → UnitType → Units
        // ========================================
        if (isQuranDomain)
        {
            return await DetermineQuranNextStepAsync(state, rule, domainId, pageNumber, pageSize);
        }

        // ========================================
        // STANDARD DOMAIN FLOW (School, Language, Skills)
        // Curriculum → Level → Grade → Subject → Term → Units
        // ========================================
        return await DetermineStandardNextStepAsync(state, rule, domainId);
    }

    /// <summary>
    /// Internal result class for filter step determination
    /// </summary>
    private class FilterStepResult
    {
        public string NextStep { get; set; } = default!;
        public List<FilterOptionDto> Options { get; set; } = new();
        public List<FilterOptionDto>? Unit { get; set; }
        public int? TotalCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }
        public List<FilterOptionDto>? QuranContentTypes { get; set; }
        public List<FilterOptionDto>? QuranLevels { get; set; }
    }

    /// <summary>
    /// Quran domain flow: Subject → QuranContentType → QuranLevel → Unit → Lesson → Done
    /// </summary>
    private async Task<FilterStepResult> DetermineQuranNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId,
        int pageNumber,
        int pageSize)
    {
        var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
            domainId,
            curriculumId: null,
            levelId: null,
            gradeId: null,
            termId: null);

        var quranSubject = subjects.FirstOrDefault();
        if (quranSubject == null)
            throw new InvalidOperationException("Quran subject not found");

        if (!state.SubjectId.HasValue)
            state.SubjectId = quranSubject.Id;

        var contentTypes = await _quranContentTypeRepository.GetQuranContentTypesAsOptionsAsync();
        var quranLevels = await _quranLevelRepository.GetQuranLevelsAsOptionsAsync();

        if (rule.RequiresQuranContentType && !state.QuranContentTypeId.HasValue)
        {
            return new FilterStepResult
            {
                NextStep = "QuranContentType",
                Options = contentTypes,
                QuranContentTypes = contentTypes,
                QuranLevels = quranLevels
            };
        }

        if (rule.RequiresQuranLevel && !state.QuranLevelId.HasValue)
        {
            return new FilterStepResult
            {
                NextStep = "QuranLevel",
                Options = quranLevels,
                QuranContentTypes = contentTypes,
                QuranLevels = quranLevels
            };
        }

        if (!state.ContentUnitId.HasValue)
        {
            var unitTypeCode = string.IsNullOrEmpty(state.UnitTypeCode)
                ? "QuranPart"
                : state.UnitTypeCode;

            var (unitOptions, totalCount) = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
                state.SubjectId.Value,
                unitTypeCode,
                pageNumber,
                pageSize);

            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

            return new FilterStepResult
            {
                NextStep = "Unit",
                Options = new List<FilterOptionDto>(),
                Unit = unitOptions,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                QuranContentTypes = contentTypes,
                QuranLevels = quranLevels
            };
        }

        await ValidateContentUnitForSubjectAsync(state);

        if (rule.HasLessons
            && !state.SkipLessons
            && (state.LessonIds == null || !state.LessonIds.Any()))
        {
            var lessons = await _lessonRepository.GetLessonsAsOptionsAsync(
                state.ContentUnitId.Value,
                state.QuranContentTypeId,
                state.QuranLevelId);

            return new FilterStepResult
            {
                NextStep = "Lesson",
                Options = lessons,
                QuranContentTypes = contentTypes,
                QuranLevels = quranLevels
            };
        }

        return new FilterStepResult
        {
            NextStep = "Done",
            Options = new List<FilterOptionDto>(),
            QuranContentTypes = contentTypes,
            QuranLevels = quranLevels
        };
    }

    /// <summary>
    /// Standard domain flow:
    /// School: Curriculum → Level → Grade → Subject → Term → Unit → Lesson → Done
    /// University: University → College → Department → AcademicProgram → Level → Subject → [Term?] → Unit → Lesson → Done
    /// </summary>
    private async Task<FilterStepResult> DetermineStandardNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId)
    {
        // University institutional prefix
        if (rule.HasUniversity && !state.UniversityId.HasValue)
        {
            var universities = await _universityRepository.GetUniversitiesAsOptionsAsync();
            return new FilterStepResult { NextStep = "University", Options = universities };
        }

        if (rule.HasCollege && !state.CollegeId.HasValue)
        {
            if (!state.UniversityId.HasValue)
                throw new InvalidOperationException("UniversityId is required before selecting College");

            var colleges = await _collegeRepository.GetCollegesAsOptionsAsync(state.UniversityId.Value);
            return new FilterStepResult { NextStep = "College", Options = colleges };
        }

        if (rule.HasDepartment && !state.DepartmentId.HasValue)
        {
            if (!state.CollegeId.HasValue)
                throw new InvalidOperationException("CollegeId is required before selecting Department");

            var departments = await _departmentRepository.GetDepartmentsAsOptionsAsync(state.CollegeId.Value);
            return new FilterStepResult { NextStep = "Department", Options = departments };
        }

        if (rule.HasAcademicProgram && !state.AcademicProgramId.HasValue)
        {
            if (!state.DepartmentId.HasValue)
                throw new InvalidOperationException("DepartmentId is required before selecting AcademicProgram");

            var programs = await _academicProgramRepository.GetProgramsAsOptionsAsync(state.DepartmentId.Value);
            return new FilterStepResult { NextStep = "AcademicProgram", Options = programs };
        }

        // Curriculum (school path)
        if (rule.HasCurriculum && !state.CurriculumId.HasValue)
        {
            var curricula = await _curriculumRepository.GetCurriculumsAsOptionsAsync(domainId);
            return new FilterStepResult { NextStep = "Curriculum", Options = curricula };
        }

        // EducationLevel
        if (rule.HasEducationLevel && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(
                domainId,
                state.CurriculumId,
                state.AcademicProgramId);
            return new FilterStepResult { NextStep = "Level", Options = levels };
        }

        // Grade
        if (rule.HasGrade && !state.GradeId.HasValue)
        {
            if (!state.LevelId.HasValue)
                throw new InvalidOperationException("LevelId is required before selecting Grade");

            var grades = await _gradeRepository.GetGradesAsOptionsAsync(state.LevelId.Value);
            return new FilterStepResult { NextStep = "Grade", Options = grades };
        }

        // Subject
        if (!state.SubjectId.HasValue)
        {
            // University: recover AcademicProgramId from Level when the client omitted it
            // (otherwise subject options filter only by LevelId and often return empty).
            if (!state.AcademicProgramId.HasValue && state.LevelId.HasValue && rule.HasAcademicProgram)
            {
                var level = await _levelRepository.GetByIdAsync(state.LevelId.Value);
                if (level?.AcademicProgramId is int programId)
                    state.AcademicProgramId = programId;
            }

            var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
                domainId,
                state.CurriculumId,
                state.LevelId,
                state.GradeId,
                termId: null,
                academicProgramId: state.AcademicProgramId);
            return new FilterStepResult { NextStep = "Subject", Options = subjects };
        }

        // AcademicTerm (optional for university)
        if (rule.HasAcademicTerm
            && !state.SkipTerm
            && (state.TermIds == null || !state.TermIds.Any()))
        {
            if (rule.AcademicTermOptional && rule.HasAcademicProgram)
            {
                // University: always surface Term so admin can select or Add (even if empty).
                if (state.AcademicProgramId.HasValue)
                {
                    var programTerms = await _termRepository.GetAcademicTermsByProgramAsOptionsAsync(state.AcademicProgramId.Value);
                    return new FilterStepResult { NextStep = "Term", Options = programTerms };
                }
            }
            else if (state.CurriculumId.HasValue)
            {
                var terms = await _termRepository.GetAcademicTermsAsOptionsAsync(state.CurriculumId.Value);
                return new FilterStepResult { NextStep = "Term", Options = terms };
            }
            else if (!rule.AcademicTermOptional)
            {
                throw new InvalidOperationException("CurriculumId or AcademicProgramId is required before selecting Term");
            }
        }

        // ContentUnits
        if (rule.HasContentUnits && !state.ContentUnitId.HasValue)
        {
            var units = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
                state.SubjectId!.Value,
                unitTypeCode: null,
                termIds: state.TermIds);
            return new FilterStepResult
            {
                NextStep = "Unit",
                Options = new List<FilterOptionDto>(),
                Unit = units,
                TotalCount = units.Count,
                PageNumber = 1,
                PageSize = units.Count,
                TotalPages = 1
            };
        }

        if (state.ContentUnitId.HasValue)
            await ValidateContentUnitForSubjectAsync(state);

        // Lessons
        if (rule.HasLessons
            && state.ContentUnitId.HasValue
            && !state.SkipLessons
            && (state.LessonIds == null || !state.LessonIds.Any()))
        {
            var lessons = await _lessonRepository.GetLessonsAsOptionsAsync(state.ContentUnitId.Value);
            return new FilterStepResult { NextStep = "Lesson", Options = lessons };
        }

        return new FilterStepResult { NextStep = "Done", Options = new List<FilterOptionDto>() };
    }

    private async Task ValidateContentUnitForSubjectAsync(FilterStateDto state)
    {
        if (!state.ContentUnitId.HasValue || !state.SubjectId.HasValue)
            return;

        var unit = await _contentUnitRepository.GetByIdAsync(state.ContentUnitId.Value);
        if (unit == null)
            throw new InvalidOperationException($"Content unit {state.ContentUnitId} not found");

        if (unit.SubjectId != state.SubjectId.Value)
            throw new ArgumentException(
                $"Content unit {state.ContentUnitId} does not belong to subject {state.SubjectId}.");
    }

    private EducationRuleDto MapToRuleDto(EducationRule rule)
    {
        return new EducationRuleDto
        {
            HasCurriculum = rule.HasCurriculum,
            HasEducationLevel = rule.HasEducationLevel,
            HasGrade = rule.HasGrade,
            HasAcademicTerm = rule.HasAcademicTerm,
            HasContentUnits = rule.HasContentUnits,
            HasLessons = rule.HasLessons,
            HasUniversity = rule.HasUniversity,
            HasCollege = rule.HasCollege,
            HasDepartment = rule.HasDepartment,
            HasAcademicProgram = rule.HasAcademicProgram,
            AcademicTermOptional = rule.AcademicTermOptional,
            RequiresQuranContentType = rule.RequiresQuranContentType,
            RequiresQuranLevel = rule.RequiresQuranLevel,
            RequiresUnitTypeSelection = rule.RequiresUnitTypeSelection,
            RulesConfigured = rule.RulesConfigured,
        };
    }
}
