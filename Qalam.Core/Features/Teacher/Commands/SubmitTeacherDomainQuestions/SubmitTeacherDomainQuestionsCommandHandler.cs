using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherDomainQuestions;

public class SubmitTeacherDomainQuestionsCommandHandler : ResponseHandler,
    IRequestHandler<SubmitTeacherDomainQuestionsCommand, Response<TeacherDomainQuestionSubmitResponseDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDomainQuestionRepository _questionRepository;
    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ITeacherDomainQuestionSubmitService _submitService;
    private readonly ITeacherDomainQuestionStatusService _statusService;
    private readonly ILogger<SubmitTeacherDomainQuestionsCommandHandler> _logger;

    public SubmitTeacherDomainQuestionsCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherDomainQuestionRepository questionRepository,
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDomainQuestionSubmitService submitService,
        ITeacherDomainQuestionStatusService statusService,
        IStringLocalizer<SharedResources> localizer,
        ILogger<SubmitTeacherDomainQuestionsCommandHandler> logger) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _questionRepository = questionRepository;
        _submissionRepository = submissionRepository;
        _submitService = submitService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Response<TeacherDomainQuestionSubmitResponseDto>> Handle(
        SubmitTeacherDomainQuestionsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<TeacherDomainQuestionSubmitResponseDto>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return BadRequest<TeacherDomainQuestionSubmitResponseDto>("Teacher profile not found.");

        var activeQuestions = await _questionRepository.GetActiveByDomainIdAsync(request.DomainId, cancellationToken);
        if (activeQuestions.Count == 0)
            return BadRequest<TeacherDomainQuestionSubmitResponseDto>("No active questions configured for this domain.");

        foreach (var q in activeQuestions)
        {
            if (await _submissionRepository.ExistsForTeacherAndQuestionAsync(teacher.Id, q.Id, cancellationToken))
            {
                return BadRequest<TeacherDomainQuestionSubmitResponseDto>(
                    $"Question '{q.Code}' was already answered and cannot be changed.");
            }
        }

        var (input, submittedCodes, normalizeError) = NormalizeRequest(request);
        if (normalizeError != null)
            return BadRequest<TeacherDomainQuestionSubmitResponseDto>(normalizeError);

        var unknownCodeError = ValidateKnownCodes(submittedCodes, activeQuestions);
        if (unknownCodeError != null)
            return BadRequest<TeacherDomainQuestionSubmitResponseDto>(unknownCodeError);

        var validationError = ValidateAgainstQuestions(input, activeQuestions);
        if (validationError != null)
            return BadRequest<TeacherDomainQuestionSubmitResponseDto>(validationError);

        try
        {
            await _submitService.SubmitAsync(teacher, input, activeQuestions, cancellationToken);

            var requiresAnswer = await _statusService.DomainRequiresAnswerAsync(teacher.Id, request.DomainId, cancellationToken);

            return Success(
                entity: new TeacherDomainQuestionSubmitResponseDto
                {
                    DomainId = request.DomainId,
                    RequiresAnswer = requiresAnswer,
                    SubmittedCodes = submittedCodes,
                    Message = requiresAnswer
                        ? "Some required questions are still unanswered."
                        : "Domain questions submitted successfully."
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Submit domain questions failed for teacherId={TeacherId}, domainId={DomainId}",
                teacher.Id, request.DomainId);
            return BadRequest<TeacherDomainQuestionSubmitResponseDto>(ex.Message);
        }
    }

    private static (TeacherDomainQuestionSubmissionInput Input, List<string> SubmittedCodes, string? Error) NormalizeRequest(
        SubmitTeacherDomainQuestionsCommand request)
    {
        if (request.Answers.Any(a => !string.IsNullOrWhiteSpace(a.Code)))
        {
            var (input, error, codes) = TeacherDomainQuestionAnswerMapper.TryMap(request.DomainId, request.Answers);
            if (error != null)
                return (null!, [], error);
            return (input!, codes, null);
        }

        var legacyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        legacyCodes.UnionWith(request.CustomFilesByCode.Keys);
        legacyCodes.UnionWith(request.TextValuesByCode.Keys);
        legacyCodes.UnionWith(request.BoolValuesByCode.Keys);
        legacyCodes.UnionWith(request.SelectionsByCode.Keys);

        return (new TeacherDomainQuestionSubmissionInput
        {
            DomainId = request.DomainId,
            CustomFilesByCode = request.CustomFilesByCode,
            TextValuesByCode = request.TextValuesByCode,
            BoolValuesByCode = request.BoolValuesByCode,
            SelectionsByCode = request.SelectionsByCode
        }, legacyCodes.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList(), null);
    }

    private static string? ValidateKnownCodes(
        IReadOnlyList<string> submittedCodes,
        List<TeacherDomainQuestion> activeQuestions)
    {
        var allowed = activeQuestions
            .Select(q => q.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unknown = submittedCodes.FirstOrDefault(c => !allowed.Contains(c));
        return unknown != null
            ? $"Unknown question code '{unknown}' for this domain."
            : null;
    }

    private static string? ValidateAgainstQuestions(
        TeacherDomainQuestionSubmissionInput input,
        List<TeacherDomainQuestion> active)
    {
        foreach (var req in active.Where(r => r.IsRequired))
        {
            switch (req.RequirementType)
            {
                case RegistrationRequirementType.File:
                    if (!input.CustomFilesByCode.TryGetValue(req.Code, out var files) || files.Count < req.MinCount)
                        return $"Requirement '{req.Code}' requires at least {req.MinCount} file(s).";
                    break;

                case RegistrationRequirementType.Text:
                    if (!input.TextValuesByCode.TryGetValue(req.Code, out var text) || string.IsNullOrWhiteSpace(text))
                        return $"Requirement '{req.Code}' is required.";
                    if (req.MaxLength.HasValue && text!.Length > req.MaxLength.Value)
                        return $"Requirement '{req.Code}' exceeds the maximum length of {req.MaxLength.Value} characters.";
                    break;

                case RegistrationRequirementType.Boolean:
                    if (!input.BoolValuesByCode.TryGetValue(req.Code, out var b) || b == null)
                        return $"Requirement '{req.Code}' is required.";
                    break;

                case RegistrationRequirementType.Selection:
                    var picked = input.SelectionsByCode.TryGetValue(req.Code, out var sel) ? sel : null;
                    if (picked == null || picked.Count < req.MinCount)
                        return $"Requirement '{req.Code}' requires at least {req.MinCount} selection(s).";
                    if (picked.Count > req.MaxCount)
                        return $"At most {req.MaxCount} selection(s) allowed for '{req.Code}'.";

                    var allowedValues = RegistrationRequirementOptionsHelper.Parse(req.OptionsJson)
                        .Select(o => o.Value)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var invalid = picked.FirstOrDefault(v => !allowedValues.Contains(v));
                    if (invalid != null)
                        return $"'{invalid}' is not a valid option for '{req.Code}'.";
                    break;
            }
        }

        return null;
    }
}
