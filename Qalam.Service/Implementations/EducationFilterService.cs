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
        var (nextStep, options, paginatedOptions, quranContentTypes, quranLevels) = await DetermineNextStepAsync(state, rule, domain, pageNumber, pageSize);
        response.NextStep = nextStep;
        response.Options = options;
        response.PaginatedOptions = paginatedOptions;
        response.QuranContentTypes = quranContentTypes;
        response.QuranLevels = quranLevels;

        // For Quran domain, set the auto-selected subject
        if (domain.Code?.ToLowerInvariant() == "quran" && options.Count == 1)
        {
            response.SelectedSubject = options[0];
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

    private async Task<(string NextStep, List<FilterOptionDto> Options, PaginatedFilterOptionsDto? PaginatedOptions, List<FilterOptionDto>? QuranContentTypes, List<FilterOptionDto>? QuranLevels)> DetermineNextStepAsync(
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
    /// Quran domain flow: Single response with everything (Subject, ContentTypes, Levels, Units)
    /// Skips all intermediate steps and returns paginated units with QuranPart as default
    /// </summary>
    private async Task<(string NextStep, List<FilterOptionDto> Options, PaginatedFilterOptionsDto? PaginatedOptions, List<FilterOptionDto>? QuranContentTypes, List<FilterOptionDto>? QuranLevels)> DetermineQuranNextStepAsync(
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
        var (options, totalCount) = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
            state.SubjectId.Value,
            unitTypeCode,
            pageNumber,
            pageSize);

        var paginatedOptions = new PaginatedFilterOptionsDto
        {
            Options = options,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Return subject as an option so it can be included in response
        return ("Unit", new List<FilterOptionDto> { quranSubject }, paginatedOptions, contentTypes, levels);
    }

    /// <summary>
    /// Standard domain flow: Curriculum → Level → Grade → Term → Subject → Units
    /// </summary>
    private async Task<(string NextStep, List<FilterOptionDto> Options, PaginatedFilterOptionsDto? PaginatedOptions, List<FilterOptionDto>? QuranContentTypes, List<FilterOptionDto>? QuranLevels)> DetermineStandardNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId)
    {
        // Step 1: Curriculum
        if (rule.HasCurriculum && !state.CurriculumId.HasValue)
        {
            var curricula = await _curriculumRepository.GetCurriculumsAsOptionsAsync(domainId);
            return ("Curriculum", curricula, null, null, null);
        }

        // Step 2: EducationLevel
        if (rule.HasEducationLevel && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(domainId, state.CurriculumId);
            return ("Level", levels, null, null, null);
        }

        // Step 3: Grade
        if (rule.HasGrade && !state.GradeId.HasValue)
        {
            if (!state.LevelId.HasValue)
                throw new InvalidOperationException("LevelId is required before selecting Grade");

            var grades = await _gradeRepository.GetGradesAsOptionsAsync(state.LevelId.Value);
            return ("Grade", grades, null, null, null);
        }

        // Step 4: AcademicTerm
        if (rule.HasAcademicTerm && !state.TermId.HasValue)
        {
            if (!state.CurriculumId.HasValue)
                throw new InvalidOperationException("CurriculumId is required before selecting Term");

            var terms = await _termRepository.GetAcademicTermsAsOptionsAsync(state.CurriculumId.Value);
            return ("Term", terms, null, null, null);
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
            return ("Subject", subjects, null, null, null);
        }

        // Step 6: ContentUnits (no pagination for standard domains)
        if (rule.HasContentUnits)
        {
            var units = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
                state.SubjectId.Value,
                unitTypeCode: null);
            return ("Unit", units, null, null, null);
        }

        // All done
        return ("Done", new List<FilterOptionDto>(), null, null, null);
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
