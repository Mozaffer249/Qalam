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

    public async Task<FilterOptionsResponseDto> GetFilterOptionsAsync(FilterStateDto state)
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

        var response = new FilterOptionsResponseDto
        {
            CurrentState = state,
            Rule = MapToRuleDto(rule),
            Options = new List<FilterOptionDto>()
        };

        // Determine next step and load options
        var (nextStep, options) = await DetermineNextStepAsync(state, rule, domain);
        response.NextStep = nextStep;
        response.Options = options;

        return response;
    }

    private async Task<(string NextStep, List<FilterOptionDto> Options)> DetermineNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        EducationDomain domain)
    {
        var domainId = domain.Id;
        var isQuranDomain = domain.Code?.ToLowerInvariant() == "quran";

        // ========================================
        // QURAN DOMAIN FLOW
        // Subject → QuranContentType → QuranLevel → Units
        // ========================================
        if (isQuranDomain)
        {
            return await DetermineQuranNextStepAsync(state, rule, domainId);
        }

        // ========================================
        // STANDARD DOMAIN FLOW (School, Language, Skills)
        // Curriculum → Level → Grade → Term → Subject → Units
        // ========================================
        return await DetermineStandardNextStepAsync(state, rule, domainId);
    }

    /// <summary>
    /// Quran domain flow: Subject → QuranContentType → QuranLevel → Units (Surahs/Parts)
    /// </summary>
    private async Task<(string NextStep, List<FilterOptionDto> Options)> DetermineQuranNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId)
    {
        // Step 1: Subject (usually single "قرآن كريم")
        if (!state.SubjectId.HasValue)
        {
            var subjects = await _subjectRepository.GetSubjectsAsOptionsAsync(
                domainId,
                curriculumId: null,
                levelId: null,
                gradeId: null,
                termId: null);
            
            // If only one subject, could auto-select, but return for explicit selection
            return ("Subject", subjects);
        }

        // Step 2: QuranContentType (تحفيظ / تلاوة / تجويد)
        if (rule.RequiresQuranContentType && !state.QuranContentTypeId.HasValue)
        {
            var contentTypes = await _quranContentTypeRepository.GetQuranContentTypesAsOptionsAsync();
            return ("QuranContentType", contentTypes);
        }

        // Step 3: QuranLevel (نورانية / مبتدئ / متوسط / متقدم)
        if (rule.RequiresQuranLevel && !state.QuranLevelId.HasValue)
        {
            var levels = await _quranLevelRepository.GetQuranLevelsAsOptionsAsync();
            return ("QuranLevel", levels);
        }

        // Step 4: ContentUnits (Surahs or Parts)
        if (rule.HasContentUnits)
        {
            var units = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
                state.SubjectId.Value,
                unitTypeCode: null); // Return both Surahs and Parts
            return ("Unit", units);
        }

        // All done
        return ("Done", new List<FilterOptionDto>());
    }

    /// <summary>
    /// Standard domain flow: Curriculum → Level → Grade → Term → Subject → Units
    /// </summary>
    private async Task<(string NextStep, List<FilterOptionDto> Options)> DetermineStandardNextStepAsync(
        FilterStateDto state,
        EducationRule rule,
        int domainId)
    {
        // Step 1: Curriculum
        if (rule.HasCurriculum && !state.CurriculumId.HasValue)
        {
            var curricula = await _curriculumRepository.GetCurriculumsAsOptionsAsync(domainId);
            return ("Curriculum", curricula);
        }

        // Step 2: EducationLevel
        if (rule.HasEducationLevel && !state.LevelId.HasValue)
        {
            var levels = await _levelRepository.GetLevelsAsOptionsAsync(domainId, state.CurriculumId);
            return ("Level", levels);
        }

        // Step 3: Grade
        if (rule.HasGrade && !state.GradeId.HasValue)
        {
            if (!state.LevelId.HasValue)
                throw new InvalidOperationException("LevelId is required before selecting Grade");

            var grades = await _gradeRepository.GetGradesAsOptionsAsync(state.LevelId.Value);
            return ("Grade", grades);
        }

        // Step 4: AcademicTerm
        if (rule.HasAcademicTerm && !state.TermId.HasValue)
        {
            if (!state.CurriculumId.HasValue)
                throw new InvalidOperationException("CurriculumId is required before selecting Term");

            var terms = await _termRepository.GetAcademicTermsAsOptionsAsync(state.CurriculumId.Value);
            return ("Term", terms);
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
            return ("Subject", subjects);
        }

        // Step 6: ContentUnits
        if (rule.HasContentUnits)
        {
            var units = await _contentUnitRepository.GetContentUnitsAsOptionsAsync(
                state.SubjectId.Value,
                unitTypeCode: null);
            return ("Unit", units);
        }

        // All done
        return ("Done", new List<FilterOptionDto>());
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
            RequiresQuranLevel = rule.RequiresQuranLevel
        };
    }
}
