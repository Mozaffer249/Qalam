using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
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
    private readonly IUniversityRepository _universityRepository;
    private readonly ICollegeRepository _collegeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAcademicProgramRepository _academicProgramRepository;
    private readonly IWritableFilterRepository _writableFilterRepository;
    private readonly IQuranContentTypeRepository _quranContentTypeRepository;

    public EducationFilterService(
        IEducationDomainRepository domainRepository,
        ICurriculumRepository curriculumRepository,
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        IAcademicTermRepository termRepository,
        ISubjectRepository subjectRepository,
        IContentUnitRepository contentUnitRepository,
        ILessonRepository lessonRepository,
        IUniversityRepository universityRepository,
        ICollegeRepository collegeRepository,
        IDepartmentRepository departmentRepository,
        IAcademicProgramRepository academicProgramRepository,
        IWritableFilterRepository writableFilterRepository,
        IQuranContentTypeRepository quranContentTypeRepository)
    {
        _domainRepository = domainRepository;
        _curriculumRepository = curriculumRepository;
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
        _termRepository = termRepository;
        _subjectRepository = subjectRepository;
        _contentUnitRepository = contentUnitRepository;
        _lessonRepository = lessonRepository;
        _universityRepository = universityRepository;
        _collegeRepository = collegeRepository;
        _departmentRepository = departmentRepository;
        _academicProgramRepository = academicProgramRepository;
        _writableFilterRepository = writableFilterRepository;
        _quranContentTypeRepository = quranContentTypeRepository;
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
        response.WritableSlotCode = result.WritableSlotCode;
        response.AllowCustomWrite = result.AllowCustomWrite;
        response.AllowSkipWritable = result.AllowSkipWritable;
        response.Unit = result.Unit;
        response.TotalCount = result.TotalCount;
        response.PageNumber = result.PageNumber;
        response.PageSize = result.PageSize;
        response.TotalPages = result.TotalPages;
        response.SelectAllByDefault = result.SelectAllByDefault;
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
        // Subject (auto) → ContentType → Riwayah → Audience → Juz → Surah → Done
        // ========================================
        if (isQuranDomain)
        {
            return await DetermineQuranNextStepAsync(state, rule, domainId, pageNumber, pageSize);
        }

        // ========================================
        // STANDARD DOMAIN FLOW
        // School: Curriculum → Level → Grade → Subject → Term → Units
        // Language: Subject → Level → Grade → Writables → Done
        // ========================================
        return await DetermineStandardNextStepAsync(state, rule, domain);
    }

    /// <summary>
    /// Internal result class for filter step determination
    /// </summary>
    private class FilterStepResult
    {
        public string NextStep { get; set; } = default!;
        public string? WritableSlotCode { get; set; }
        public bool AllowCustomWrite { get; set; }
        public bool AllowSkipWritable { get; set; }
        public bool SelectAllByDefault { get; set; }
        public List<FilterOptionDto> Options { get; set; } = new();
        public List<FilterOptionDto>? Unit { get; set; }
        public int? TotalCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }
    }

    /// <summary>
    /// Quran: Subject (auto) → ContentType → Riwayah → Audience Level → Juz → Surah → Done.
    /// Excel note: الأصل الكل for juz/surah (SelectAllByDefault); client may deselect.
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

        if (rule.RequiresQuranContentType && !state.QuranContentTypeId.HasValue)
        {
            var types = await _quranContentTypeRepository.GetQuranContentTypesAsOptionsAsync();
            return new FilterStepResult
            {
                NextStep = "QuranContentType",
                Options = types
            };
        }

        var afterSubjectWritable = await TryWritableStepAsync(
            state, rule, domainId, WritableFilterAfterSteps.Subject);
        if (afterSubjectWritable != null)
            return afterSubjectWritable;

        if (rule.HasEducationLevel && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(
                domainId,
                curriculumId: null,
                academicProgramId: null);
            return new FilterStepResult { NextStep = "Level", Options = levels };
        }

        if (rule.HasContentUnits && !state.SkipUnits)
        {
            var partDone = await HasCompletedQuranUnitTypeAsync(state, "QuranPart");
            if (!partDone)
            {
                state.UnitTypeCode = "QuranPart";
                return await BuildQuranUnitStepAsync(state, "QuranPart", pageNumber, pageSize);
            }

            var surahDone = await HasCompletedQuranUnitTypeAsync(state, "QuranSurah");
            if (!surahDone)
            {
                state.UnitTypeCode = "QuranSurah";
                return await BuildQuranUnitStepAsync(state, "QuranSurah", pageNumber, pageSize);
            }
        }

        if (state.ContentUnitId.HasValue)
            await ValidateContentUnitForSubjectAsync(state);

        if (rule.HasLessons
            && !state.SkipLessons
            && (state.LessonIds == null || !state.LessonIds.Any()))
        {
            var lessonUnitId = state.ContentUnitId
                ?? state.ContentUnitIds?.FirstOrDefault();
            if (lessonUnitId is > 0)
            {
                var lessons = await _lessonRepository.GetLessonsAsOptionsAsync(
                    lessonUnitId.Value,
                    state.QuranContentTypeId,
                    state.QuranLevelId);

                return new FilterStepResult
                {
                    NextStep = "Lesson",
                    Options = lessons
                };
            }
        }

        return new FilterStepResult
        {
            NextStep = "Done",
            Options = new List<FilterOptionDto>()
        };
    }

    private async Task<FilterStepResult> BuildQuranUnitStepAsync(
        FilterStateDto state,
        string unitTypeCode,
        int pageNumber,
        int pageSize)
    {
        var (unitOptions, totalCount) = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
            state.SubjectId!.Value,
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
            SelectAllByDefault = true
        };
    }

    private async Task<bool> HasCompletedQuranUnitTypeAsync(FilterStateDto state, string unitTypeCode)
    {
        if (state.SkipUnits)
            return true;

        var selectedIds = new HashSet<int>();
        if (state.ContentUnitIds is { Count: > 0 })
        {
            foreach (var id in state.ContentUnitIds)
                selectedIds.Add(id);
        }
        if (state.ContentUnitId.HasValue)
            selectedIds.Add(state.ContentUnitId.Value);

        if (selectedIds.Count == 0)
            return false;

        return await _contentUnitRepository.GetContentUnitsBySubjectId(state.SubjectId!.Value)
            .AnyAsync(cu =>
                cu.IsActive &&
                cu.UnitTypeCode == unitTypeCode &&
                selectedIds.Contains(cu.Id));
    }

    /// <summary>
    /// Standard domain flow:
    /// School: Curriculum → Level → Grade → Subject → Term → Unit → Lesson → Done
    /// University: University → College → Department → AcademicProgram → Level → Subject → [Term?] → Unit → Lesson → Done
    /// </summary>
    private async Task<FilterStepResult> DetermineStandardNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        EducationDomain domain)
    {
        var domainId = domain.Id;
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

        var startWritable = await TryWritableStepAsync(state, rule, domainId, WritableFilterAfterSteps.Start);
        if (startWritable != null)
            return startWritable;

        // EducationLevel (before subject — school / university)
        if (rule.HasEducationLevel && !rule.EducationLevelAfterSubject && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(
                domainId,
                state.CurriculumId,
                state.AcademicProgramId);
            return new FilterStepResult { NextStep = "Level", Options = levels };
        }

        // Grade (before subject — only when level comes before subject)
        if (rule.HasGrade && !rule.EducationLevelAfterSubject && !state.GradeId.HasValue)
        {
            if (!state.LevelId.HasValue)
                throw new InvalidOperationException("LevelId is required before selecting Grade");

            var grades = await _gradeRepository.GetGradesAsOptionsAsync(state.LevelId.Value);
            return new FilterStepResult { NextStep = "Grade", Options = grades };
        }

        if (rule.HasParentSubject && !state.ParentSubjectId.HasValue && !state.SubjectId.HasValue)
        {
            var parents = await _subjectRepository.GetSubjectsAsOptionsAsync(
                domainId,
                state.CurriculumId,
                levelId: rule.EducationLevelAfterSubject ? null : state.LevelId,
                state.GradeId,
                termId: null,
                academicProgramId: state.AcademicProgramId,
                parentsOnly: true);
            parents = FilterShariaParentSubjects(domain, parents);
            return new FilterStepResult { NextStep = "ParentSubject", Options = parents };
        }

        var afterParentWritable = await TryWritableStepAsync(state, rule, domainId, WritableFilterAfterSteps.ParentSubject);
        if (afterParentWritable != null)
            return afterParentWritable;

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

            if (rule.HasParentSubject && state.ParentSubjectId.HasValue)
            {
                var children = await _subjectRepository.GetSubjectsAsOptionsAsync(
                    domainId,
                    state.CurriculumId,
                    levelId: null,
                    gradeId: null,
                    termId: null,
                    parentSubjectId: state.ParentSubjectId);
                if (children.Count == 0)
                {
                    state.SubjectId = state.ParentSubjectId;
                }
                else
                {
                    return new FilterStepResult { NextStep = "Subject", Options = children };
                }
            }

            if (!state.SubjectId.HasValue)
            {
                var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
                    domainId,
                    state.CurriculumId,
                    levelId: rule.EducationLevelAfterSubject ? null : state.LevelId,
                    gradeId: rule.EducationLevelAfterSubject ? null : state.GradeId,
                    termId: null,
                    academicProgramId: state.AcademicProgramId);

                if (subjects.Count == 1 && rule.HasWritableFilters)
                {
                    state.SubjectId = subjects[0].Id;
                }
                else
                {
                    return new FilterStepResult { NextStep = "Subject", Options = subjects };
                }
            }
        }

        var afterSubjectWritable = await TryWritableStepAsync(state, rule, domainId, WritableFilterAfterSteps.Subject);
        if (afterSubjectWritable != null)
            return afterSubjectWritable;

        if (rule.HasEducationLevel && rule.EducationLevelAfterSubject && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(
                domainId,
                state.CurriculumId,
                state.AcademicProgramId);
            return new FilterStepResult { NextStep = "Level", Options = levels };
        }

        // Grade after subject (language: age band → CEFR)
        if (rule.HasGrade && rule.EducationLevelAfterSubject && !state.GradeId.HasValue)
        {
            if (!state.LevelId.HasValue)
                throw new InvalidOperationException("LevelId is required before selecting Grade");

            var grades = await _gradeRepository.GetGradesAsOptionsAsync(state.LevelId.Value);
            return new FilterStepResult { NextStep = "Grade", Options = grades };
        }

        var afterGradeWritable = await TryWritableStepAsync(state, rule, domainId, WritableFilterAfterSteps.Grade);
        if (afterGradeWritable != null)
            return afterGradeWritable;

        var afterLevelWritable = await TryWritableStepAsync(state, rule, domainId, WritableFilterAfterSteps.Level);
        if (afterLevelWritable != null)
            return afterLevelWritable;

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
            HasParentSubject = rule.HasParentSubject,
            EducationLevelAfterSubject = rule.EducationLevelAfterSubject,
            HasWritableFilters = rule.HasWritableFilters,
            RulesConfigured = rule.RulesConfigured,
        };
    }

    private async Task<FilterStepResult?> TryWritableStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId,
        string afterStep)
    {
        if (!rule.HasWritableFilters)
            return null;

        var slots = await _writableFilterRepository.GetActiveSlotsByDomainIdAsync(domainId);
        var selected = await _writableFilterRepository.GetByIdsAsync(state.WritableValueIds ?? []);
        var selectedSlotIds = selected.Select(v => v.SlotId).ToHashSet();
        var skipped = new HashSet<string>(
            state.SkippedWritableSlotCodes ?? [],
            StringComparer.OrdinalIgnoreCase);

        string? subjectCode = null;
        if (state.SubjectId.HasValue)
        {
            var subject = await _subjectRepository.GetByIdAsync(state.SubjectId.Value);
            subjectCode = subject?.Code;
        }

        foreach (var slot in slots.Where(s =>
                     string.Equals(s.AfterStep, afterStep, StringComparison.OrdinalIgnoreCase)))
        {
            if (selectedSlotIds.Contains(slot.Id) || skipped.Contains(slot.Code))
                continue;

            var required = slot.IsRequired
                || (!string.IsNullOrWhiteSpace(slot.RequiredWhenSubjectCodeContains)
                    && !string.IsNullOrWhiteSpace(subjectCode)
                    && subjectCode.Contains(slot.RequiredWhenSubjectCodeContains, StringComparison.OrdinalIgnoreCase));

            if (!required && string.IsNullOrWhiteSpace(slot.RequiredWhenSubjectCodeContains) && !slot.IsRequired)
            {
                // Optional slot: still surface so the client can pick or skip.
            }
            else if (!required && !string.IsNullOrWhiteSpace(slot.RequiredWhenSubjectCodeContains))
            {
                continue;
            }

            var options = await _writableFilterRepository.GetValuesAsOptionsAsync(slot.Id, subjectCode);
            return new FilterStepResult
            {
                NextStep = "WritableFilter",
                Options = options,
                WritableSlotCode = slot.Code,
                AllowCustomWrite = true,
                AllowSkipWritable = !required
            };
        }

        return null;
    }

    private static List<FilterOptionDto> FilterShariaParentSubjects(EducationDomain domain, List<FilterOptionDto> parents)
    {
        if (!string.Equals(domain.Code, EducationDomainCodes.Sharia, StringComparison.OrdinalIgnoreCase))
            return parents;

        var hasExcelCategories = parents.Any(p =>
            p.Code?.StartsWith("sharia.category.", StringComparison.OrdinalIgnoreCase) == true);
        if (!hasExcelCategories)
            return parents;

        return parents
            .Where(p => p.Code?.StartsWith("sharia.category.", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
    }
}
