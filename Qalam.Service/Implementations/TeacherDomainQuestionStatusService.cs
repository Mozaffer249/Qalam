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

    public TeacherDomainQuestionStatusService(
        ITeacherDomainQuestionRepository questionRepository,
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDomainQuestionProvider provider,
        ISubjectService subjectService)
    {
        _questionRepository = questionRepository;
        _submissionRepository = submissionRepository;
        _provider = provider;
        _subjectService = subjectService;
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
                .Any(q => !submissionByQuestionId.ContainsKey(q.Id));

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
        var submittedIds = submissions.Select(s => s.QuestionId).ToHashSet();

        return questions.Any(q => q.IsRequired && !submittedIds.Contains(q.Id));
    }

    public async Task<string?> ValidateSubjectsDomainQuestionsAsync(
        int teacherId,
        IEnumerable<int> subjectIds,
        CancellationToken cancellationToken = default)
    {
        var domainInfos = await _subjectService.GetDomainsForSubjectIdsAsync(subjectIds);
        if (domainInfos.Count == 0)
            return null;

        var distinctDomainIds = domainInfos.Select(d => d.DomainId).Distinct().ToList();
        var questions = await _questionRepository.GetActiveByDomainIdsAsync(distinctDomainIds, cancellationToken);
        var requiredByDomain = questions
            .Where(q => q.IsRequired)
            .GroupBy(q => q.DomainId)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (requiredByDomain.Count == 0)
            return null;

        var submissions = await _submissionRepository.GetByTeacherIdAsync(teacherId, cancellationToken);
        var submittedQuestionIds = submissions.Select(s => s.QuestionId).ToHashSet();

        foreach (var info in domainInfos.DistinctBy(d => d.DomainId))
        {
            if (!requiredByDomain.TryGetValue(info.DomainId, out var required))
                continue;

            if (required.Any(q => !submittedQuestionIds.Contains(q.Id)))
            {
                return $"Complete domain questions for '{info.DomainNameEn}' ({info.DomainCode}) before adding subjects.";
            }
        }

        return null;
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
