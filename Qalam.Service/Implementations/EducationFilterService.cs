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
    private readonly IQuranContentTypeRepository _quranContentTypeRepository;
    private readonly IQuranLevelRepository _quranLevelRepository;

    public EducationFilterService(
        IEducationDomainRepository domainRepository,
        ICurriculumRepository curriculumRepository,
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        IAcademicTermRepository termRepository,
        ISubjectRepository subjectRepository,
        IContentUnitRepository contentUnitRepository,
        IQuranContentTypeRepository quranContentTypeRepository,
        IQuranLevelRepository quranLevelRepository)
    {
        _domainRepository = domainRepository;
        _curriculumRepository = curriculumRepository;
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
        _termRepository = termRepository;
        _subjectRepository = subjectRepository;
        _contentUnitRepository = contentUnitRepository;
        _quranContentTypeRepository = quranContentTypeRepository;
        _quranLevelRepository = quranLevelRepository;
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

        // For Quran domain, set the auto-selected subject
        if (domain.Code?.ToLowerInvariant() == "quran" && result.Options.Count == 1)
        {
            response.SelectedSubject = result.Options[0];
            response.Options = new List<FilterOptionDto>(); // Clear since we're using SelectedSubject

            // Auto-populate SubjectId in state if not already set
            if (!state.SubjectId.HasValue && response.SelectedSubject != null)
            {
                state.SubjectId = response.SelectedSubject.Id;
                response.CurrentState.SubjectId = response.SelectedSubject.Id;
            }
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
        // Curriculum → Level → Grade → Term → Subject → Units
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
    /// Quran domain flow: Single response with everything (Subject, ContentTypes, Levels, Units)
    /// Skips all intermediate steps and returns paginated units with QuranPart as default
    /// </summary>
    private async Task<FilterStepResult> DetermineQuranNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId,
        int pageNumber,
        int pageSize)
    {
        // Always fetch the single Quran subject
        var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
            domainId,
            curriculumId: null,
            levelId: null,
            gradeId: null,
            termId: null);

        var quranSubject = subjects.FirstOrDefault();
        if (quranSubject == null)
            throw new InvalidOperationException("Quran subject not found");

        // Set subject in state if not provided
        if (!state.SubjectId.HasValue)
            state.SubjectId = quranSubject.Id;

        // Default to QuranPart if no UnitTypeCode specified
        var unitTypeCode = string.IsNullOrEmpty(state.UnitTypeCode)
            ? "QuranPart"
            : state.UnitTypeCode;

        // Fetch all data sequentially (DbContext is not thread-safe for parallel operations)
        var contentTypes = await _quranContentTypeRepository.GetQuranContentTypesAsOptionsAsync();
        var levels = await _quranLevelRepository.GetQuranLevelsAsOptionsAsync();
        var (unitOptions, totalCount) = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
            state.SubjectId.Value,
            unitTypeCode,
            pageNumber,
            pageSize);

        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

        // Return subject as an option so it can be included in response
        return new FilterStepResult
        {
            NextStep = "Unit",
            Options = new List<FilterOptionDto> { quranSubject },
            Unit = unitOptions,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            QuranContentTypes = contentTypes,
            QuranLevels = levels
        };
    }

    /// <summary>
    /// Standard domain flow: Curriculum → Level → Grade → Term → Subject → Units
    /// </summary>
    private async Task<FilterStepResult> DetermineStandardNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId)
    {
        // Step 1: Curriculum
        if (rule.HasCurriculum && !state.CurriculumId.HasValue)
        {
            var curricula = await _curriculumRepository.GetCurriculumsAsOptionsAsync(domainId);
            return new FilterStepResult { NextStep = "Curriculum", Options = curricula };
        }

        // Step 2: EducationLevel
        if (rule.HasEducationLevel && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(domainId, state.CurriculumId);
            return new FilterStepResult { NextStep = "Level", Options = levels };
        }

        // Step 3: Grade
        if (rule.HasGrade && !state.GradeId.HasValue)
        {
            if (!state.LevelId.HasValue)
                throw new InvalidOperationException("LevelId is required before selecting Grade");

            var grades = await _gradeRepository.GetGradesAsOptionsAsync(state.LevelId.Value);
            return new FilterStepResult { NextStep = "Grade", Options = grades };
        }

        // Step 4: AcademicTerm
        if (rule.HasAcademicTerm && !state.TermId.HasValue)
        {
            if (!state.CurriculumId.HasValue)
                throw new InvalidOperationException("CurriculumId is required before selecting Term");

            var terms = await _termRepository.GetAcademicTermsAsOptionsAsync(state.CurriculumId.Value);
            return new FilterStepResult { NextStep = "Term", Options = terms };
        }

        // Step 5: Subject
        if (!state.SubjectId.HasValue)
        {
            var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
                domainId,
                state.CurriculumId,
                state.LevelId,
                state.GradeId,
                state.TermId);
            return new FilterStepResult { NextStep = "Subject", Options = subjects };
        }

        // Step 6: ContentUnits (no pagination for standard domains - return as unit array)
        if (rule.HasContentUnits)
        {
            var units = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
                state.SubjectId.Value,
                unitTypeCode: null);
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

        // All done
        return new FilterStepResult { NextStep = "Done", Options = new List<FilterOptionDto>() };
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
            RequiresQuranContentType = rule.RequiresQuranContentType,
            RequiresQuranLevel = rule.RequiresQuranLevel,
            RequiresUnitTypeSelection = rule.RequiresUnitTypeSelection
        };
    }
}
