using Qalam.Data.DTOs;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherDomainQuestionStatusService : ITeacherDomainQuestionStatusService
{
    private readonly ITeacherDomainQuestionRepository _questionRepository;
    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ITeacherDomainQuestionProvider _provider;
    private readonly ISubjectService _subjectService;
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly IEducationDomainService _educationDomainService;
    private readonly ITeacherDomainSubjectCascadeService _cascadeService;

    public TeacherDomainQuestionStatusService(
        ITeacherDomainQuestionRepository questionRepository,
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDomainQuestionProvider provider,
        ISubjectService subjectService,
        ITeacherSubjectRepository teacherSubjectRepository,
        IEducationDomainService educationDomainService,
        ITeacherDomainSubjectCascadeService cascadeService)
    {
        _questionRepository = questionRepository;
        _submissionRepository = submissionRepository;
        _provider = provider;
        _subjectService = subjectService;
        _teacherSubjectRepository = teacherSubjectRepository;
        _educationDomainService = educationDomainService;
        _cascadeService = cascadeService;
    }

    public async Task<List<EducationDomainTeacherDto>> EnrichDomainsForTeacherAsync(
        IReadOnlyList<EducationDomainDto> domains,
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        if (domains.Count == 0)
            return new List<EducationDomainTeacherDto>();

        var domainIds = domains.Select(d => d.Id).ToList();
        var questions = await _questionRepository.GetActiveByDomainIdsAsync(domainIds, cancellationToken);
        var submissions = await _submissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var submissionByQuestionId = submissions.ToDictionary(s => s.QuestionId);

        var questionsByDomain = questions.GroupBy(q => q.DomainId).ToDictionary(g => g.Key, g => g.ToList());

        return domains.Select(domain =>
        {
            var domainQuestions = questionsByDomain.GetValueOrDefault(domain.Id) ?? new List<Data.Entity.Teacher.TeacherDomainQuestion>();
            var publicQuestions = domainQuestions
                .Select(q => _provider.ToPublicDto(q, submissionByQuestionId.GetValueOrDefault(q.Id)))
                .ToList();

            var requiresAnswer = domainQuestions
                .Where(q => q.IsRequired)
                .Any(q => QuestionNeedsAnswer(q.Id, submissionByQuestionId));

            return new EducationDomainTeacherDto
            {
                Id = domain.Id,
                NameAr = domain.NameAr,
                NameEn = domain.NameEn,
                Code = domain.Code,
                DescriptionAr = domain.DescriptionAr,
                DescriptionEn = domain.DescriptionEn,
                CreatedAt = domain.CreatedAt,
                RequiresAnswer = requiresAnswer,
                Questions = publicQuestions
            };
        }).ToList();
    }

    public async Task<bool> DomainRequiresAnswerAsync(int teacherId, int domainId, CancellationToken cancellationToken = default)
    {
        var questions = await _questionRepository.GetActiveByDomainIdAsync(domainId, cancellationToken);
        if (questions.Count == 0)
            return false;

        var submissions = await _submissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var submissionByQuestionId = submissions.ToDictionary(s => s.QuestionId);

        return questions.Any(q => q.IsRequired && QuestionNeedsAnswer(q.Id, submissionByQuestionId));
    }

    public async Task<string?> ValidateSubjectsDomainQuestionsAsync(
        int teacherId,
        IEnumerable<int> subjectIds,
        CancellationToken cancellationToken = default)
    {
        var domainInfos = await _subjectService.GetDomainsForSubjectIdsAsync(subjectIds);
        if (domainInfos.Count == 0)
            return null;

        foreach (var info in domainInfos.DistinctBy(d => d.DomainId))
        {
            var blockReason = await _cascadeService.GetSubjectSaveBlockReasonForDomainAsync(
                teacherId,
                info.DomainId,
                info.DomainNameEn,
                info.DomainCode,
                cancellationToken);
            if (blockReason != null)
                return blockReason;
        }

        return null;
    }

    public async Task<TeacherDomainQuestionStatusResponseDto> GetDomainQuestionStatusAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var domainIds = await GetRelevantDomainIdsAsync(teacherId, cancellationToken);
        if (domainIds.Count == 0)
            return new TeacherDomainQuestionStatusResponseDto();

        var questions = await _questionRepository.GetActiveByDomainIdsAsync(domainIds, cancellationToken);
        var submissions = await _submissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var submissionByQuestionId = submissions.ToDictionary(s => s.QuestionId);

        var questionsByDomain = questions.GroupBy(q => q.DomainId).ToDictionary(g => g.Key, g => g.ToList());
        var domainMeta = await BuildDomainMetaAsync(domainIds, cancellationToken);

        var domains = domainIds.Select(domainId =>
        {
            var domainQuestions = questionsByDomain.GetValueOrDefault(domainId) ?? [];
            var meta = domainMeta[domainId];
            var publicQuestions = domainQuestions
                .Select(q => _provider.ToPublicDto(q, submissionByQuestionId.GetValueOrDefault(q.Id)))
                .ToList();

            var requiresAnswer = domainQuestions
                .Where(q => q.IsRequired)
                .Any(q => QuestionNeedsAnswer(q.Id, submissionByQuestionId));

            var pendingCorrections = domainQuestions
                .Where(q => submissionByQuestionId.TryGetValue(q.Id, out var sub)
                            && sub.VerificationStatus == DocumentVerificationStatus.Rejected)
                .Select(q =>
                {
                    var sub = submissionByQuestionId[q.Id];
                    return new TeacherDomainQuestionCorrectionDto
                    {
                        SubmissionId = sub.Id,
                        QuestionCode = q.Code,
                        QuestionNameEn = q.NameEn,
                        RejectionReason = sub.RejectionReason ?? string.Empty
                    };
                })
                .ToList();

            return new TeacherDomainQuestionStatusDomainDto
            {
                DomainId = domainId,
                DomainCode = meta.Code,
                NameAr = meta.NameAr,
                NameEn = meta.NameEn,
                RequiresAnswer = requiresAnswer,
                HasRejectedAnswers = pendingCorrections.Count > 0,
                PendingCorrections = pendingCorrections,
                Questions = publicQuestions
            };
        }).OrderBy(d => d.NameEn).ToList();

        return new TeacherDomainQuestionStatusResponseDto { Domains = domains };
    }

    public async Task<bool> HasRejectedDomainQuestionsAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var submissions = await _submissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        return submissions.Any(s => s.VerificationStatus == DocumentVerificationStatus.Rejected);
    }

    public Task<List<int>> GetCatalogDomainIdsWithRequiredQuestionsAsync(CancellationToken cancellationToken = default) =>
        _questionRepository.GetDomainIdsWithActiveRequiredQuestionsAsync(cancellationToken);

    public async Task<bool> HasIncompleteCatalogDomainAnswersAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var catalogDomainIds = await GetCatalogDomainIdsWithRequiredQuestionsAsync(cancellationToken);
        if (catalogDomainIds.Count == 0)
            return false;

        var questions = await _questionRepository.GetActiveByDomainIdsAsync(catalogDomainIds, cancellationToken);
        var required = questions.Where(q => q.IsRequired).ToList();
        if (required.Count == 0)
            return false;

        var submissions = await _submissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var submissionByQuestionId = submissions.ToDictionary(s => s.QuestionId);

        return required.Any(q => !submissionByQuestionId.ContainsKey(q.Id));
    }

    public async Task<bool> AreAllCatalogDomainsFullyApprovedAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var catalogDomainIds = await GetCatalogDomainIdsWithRequiredQuestionsAsync(cancellationToken);
        if (catalogDomainIds.Count == 0)
            return true;

        foreach (var domainId in catalogDomainIds)
        {
            if (!await _cascadeService.IsDomainFullyApprovedForTeacherAsync(teacherId, domainId, cancellationToken))
                return false;
        }

        return true;
    }

    public async Task<bool> HasCatalogDomainsPendingAdminReviewAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var catalogDomainIds = await GetCatalogDomainIdsWithRequiredQuestionsAsync(cancellationToken);
        if (catalogDomainIds.Count == 0)
            return false;

        if (await HasIncompleteCatalogDomainAnswersAsync(teacherId, cancellationToken))
            return false;

        if (await HasRejectedDomainQuestionsAsync(teacherId, cancellationToken))
            return false;

        return !await AreAllCatalogDomainsFullyApprovedAsync(teacherId, cancellationToken);
    }

    public async Task<bool> HasAnyDomainRequiringAnswerAsync(int teacherId, CancellationToken cancellationToken = default) =>
        await HasIncompleteCatalogDomainAnswersAsync(teacherId, cancellationToken);

    private async Task<List<int>> GetRelevantDomainIdsAsync(int teacherId, CancellationToken cancellationToken) =>
        await GetCatalogDomainIdsWithRequiredQuestionsAsync(cancellationToken);

    private async Task<Dictionary<int, (string Code, string NameAr, string NameEn)>> BuildDomainMetaAsync(
        List<int> domainIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, (string Code, string NameAr, string NameEn)>();
        foreach (var domainId in domainIds)
        {
            var dto = await _educationDomainService.GetDomainDtoByIdAsync(domainId);
            if (dto != null)
            {
                result[domainId] = (dto.Code, dto.NameAr, dto.NameEn);
                continue;
            }

            result[domainId] = ($"domain-{domainId}", $"Domain {domainId}", $"Domain {domainId}");
        }

        return result;
    }

    private static bool QuestionNeedsAnswer(
        int questionId,
        Dictionary<int, Data.Entity.Teacher.TeacherDomainQuestionSubmission> submissionByQuestionId)
    {
        if (!submissionByQuestionId.TryGetValue(questionId, out var sub))
            return true;

        return sub.VerificationStatus == DocumentVerificationStatus.Rejected;
    }

    public async Task<List<TeacherDomainQuestionGroupDto>> GetChecklistForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var submissions = await _submissionRepository.GetByTeacherIdWithQuestionsAsync(teacherId, cancellationToken);
        if (submissions.Count == 0)
            return new List<TeacherDomainQuestionGroupDto>();

        return submissions
            .GroupBy(s => new { s.Question.DomainId, s.Question.Domain.Code, s.Question.Domain.NameAr, s.Question.Domain.NameEn })
            .Select(g => new TeacherDomainQuestionGroupDto
            {
                DomainId = g.Key.DomainId,
                DomainCode = g.Key.Code,
                DomainNameAr = g.Key.NameAr,
                DomainNameEn = g.Key.NameEn,
                Questions = g.Select(s => MapSubmissionStatus(s)).OrderBy(q => q.Code).ToList()
            })
            .OrderBy(g => g.DomainNameEn)
            .ToList();
    }

    private static TeacherDomainQuestionSubmissionStatusDto MapSubmissionStatus(
        Data.Entity.Teacher.TeacherDomainQuestionSubmission submission)
    {
        var q = submission.Question;
        List<RequirementOptionDto>? selectedOptions = null;

        if (q.RequirementType == RegistrationRequirementType.Selection && !string.IsNullOrWhiteSpace(submission.TextValue))
        {
            var allowed = RegistrationRequirementOptionsHelper.Parse(q.OptionsJson);
            var picked = submission.TextValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            selectedOptions = allowed
                .Where(o => picked.Contains(o.Value, StringComparer.OrdinalIgnoreCase))
                .Select(o => new RequirementOptionDto { Value = o.Value, LabelAr = o.LabelAr, LabelEn = o.LabelEn })
                .ToList();
        }

        return new TeacherDomainQuestionSubmissionStatusDto
        {
            SubmissionId = submission.Id,
            QuestionId = q.Id,
            Code = q.Code,
            NameAr = q.NameAr,
            NameEn = q.NameEn,
            RequirementType = q.RequirementType.ToString(),
            IsRequired = q.IsRequired,
            RequiresAdminReview = q.RequiresAdminReview,
            IsSubmitted = true,
            VerificationStatus = submission.VerificationStatus,
            RejectionReason = submission.RejectionReason,
            TeacherDocumentId = submission.TeacherDocumentId,
            TextValue = submission.TextValue,
            BoolValue = submission.BoolValue,
            SelectedOptions = selectedOptions
        };
    }
}
