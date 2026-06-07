using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Validators;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;

public class SubmitTeacherRegistrationRequirementsCommandHandler : ResponseHandler,
    IRequestHandler<SubmitTeacherRegistrationRequirementsCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationRequirementRepository _requirementRepository;
    private readonly ITeacherRegistrationSubmitService _submitService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
    private readonly ILogger<SubmitTeacherRegistrationRequirementsCommandHandler> _logger;

    public SubmitTeacherRegistrationRequirementsCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmitService submitService,
        ITeacherRegistrationService teacherRegistrationService,
        IStringLocalizer<SharedResources> sharedLocalizer,
        IStringLocalizer<AuthenticationResources> authLocalizer,
        ILogger<SubmitTeacherRegistrationRequirementsCommandHandler> logger) : base(sharedLocalizer)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _requirementRepository = requirementRepository;
        _submitService = submitService;
        _teacherRegistrationService = teacherRegistrationService;
        _authLocalizer = authLocalizer;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        SubmitTeacherRegistrationRequirementsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<string>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return BadRequest<string>("Teacher profile not found. Please complete personal information first.");

        if (teacher.Status == TeacherStatus.PendingVerification)
            return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.DocumentsAlreadyPendingVerification]);
        if (teacher.Status == TeacherStatus.Active)
            return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.AccountAlreadyVerified]);
        if (teacher.Status == TeacherStatus.Blocked)
            return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.AccountBlocked]);

        var activeRequirements = await _requirementRepository.GetActiveOrderedAsync(cancellationToken);
        if (activeRequirements.Count == 0)
            return BadRequest<string>("No active registration requirements configured.");

        var validationError = ValidateAgainstRequirements(request, activeRequirements);
        if (validationError != null)
            return BadRequest<string>(validationError);

        // Identity business rules — run BEFORE delegating so we reject with a localized message.
        // Exclude this teacher's own rows so a previous partial attempt doesn't flag the retry as a duplicate.
        var identityRequired = activeRequirements.Any(r =>
            r.Code == TeacherRegistrationRequirementCodes.IdentityDocument && r.IsRequired);
        if (identityRequired && request.IdentityDocumentFile != null)
        {
            try
            {
                TeacherDocumentBusinessRules.ValidateSaudiIdentityRules(
                    request.IsInSaudiArabia ?? false, request.IdentityType, request.IssuingCountryCode, _authLocalizer);

                await TeacherDocumentBusinessRules.ValidateIdentityUnique(
                    _documentRepository, request.IdentityType, request.DocumentNumber,
                    request.IssuingCountryCode, _authLocalizer, excludeTeacherId: teacher.Id);
            }
            catch (ValidationException vex)
            {
                return BadRequest<string>(vex.Message);
            }
        }

        try
        {
            await _submitService.SubmitAsync(teacher, MapToInput(request), activeRequirements, cancellationToken);
            await _teacherRegistrationService.CompleteDocumentUploadAsync(teacher.Id, request.IsInSaudiArabia ?? false);
            return Success<string>("Registration submitted successfully. Your information is pending verification.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SubmitRegistrationRequirements handler caught teacherId={TeacherId}, userId={UserId}",
                teacher.Id, request.UserId);
            return BadRequest<string>(ex.Message);
        }
    }

    private static TeacherRegistrationSubmissionInput MapToInput(SubmitTeacherRegistrationRequirementsCommand request) =>
        new()
        {
            IsInSaudiArabia = request.IsInSaudiArabia,
            Bio = request.Bio,
            IdentityType = request.IdentityType,
            DocumentNumber = request.DocumentNumber,
            IssuingCountryCode = request.IssuingCountryCode,
            IdentityDocumentFile = request.IdentityDocumentFile,
            Certificates = request.Certificates,
            CustomFilesByCode = request.CustomFilesByCode,
            TextValuesByCode = request.TextValuesByCode,
            BoolValuesByCode = request.BoolValuesByCode,
            SelectionsByCode = request.SelectionsByCode
        };

    /// <summary>
    /// Validates the submit body against the admin-controlled catalog. Identity and certificate
    /// are special-cased because their wire shapes are structurally non-generic (multi-field tuple
    /// + list-of-tuples). Everything else dispatches off <c>RequirementType</c> + a code-keyed
    /// dictionary on the command — so admins can add Text / Boolean / Selection / custom-File
    /// requirements without a new validator branch.
    /// </summary>
    private static string? ValidateAgainstRequirements(
        SubmitTeacherRegistrationRequirementsCommand request,
        List<TeacherRegistrationRequirement> active)
    {
        foreach (var req in active.Where(r => r.IsRequired))
        {
            if (req.Code == TeacherRegistrationRequirementCodes.IdentityDocument)
            {
                if (request.IdentityDocumentFile == null)
                    return "Identity document is required.";
                continue;
            }
            if (req.Code == TeacherRegistrationRequirementCodes.Certificate)
            {
                if (request.Certificates.Count < req.MinCount)
                    return $"At least {req.MinCount} certificate(s) required.";
                if (request.Certificates.Any(c => c.File == null))
                    return "Certificate file is required.";
                continue;
            }

            switch (req.RequirementType)
            {
                case RegistrationRequirementType.File:
                    if (!request.CustomFilesByCode.TryGetValue(req.Code, out var files) || files.Count < req.MinCount)
                        return $"Requirement '{req.Code}' requires at least {req.MinCount} file(s).";
                    break;

                case RegistrationRequirementType.Text:
                    if (!request.TextValuesByCode.TryGetValue(req.Code, out var text) || string.IsNullOrWhiteSpace(text))
                        return $"Requirement '{req.Code}' is required.";
                    if (req.MaxLength.HasValue && text!.Length > req.MaxLength.Value)
                        return $"Requirement '{req.Code}' exceeds the maximum length of {req.MaxLength.Value} characters.";
                    break;

                case RegistrationRequirementType.Boolean:
                    if (!request.BoolValuesByCode.TryGetValue(req.Code, out var b) || b == null)
                        return $"Requirement '{req.Code}' is required.";
                    break;

                case RegistrationRequirementType.Selection:
                    var picked = request.SelectionsByCode.TryGetValue(req.Code, out var sel) ? sel : null;
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

        var certReq = active.FirstOrDefault(r => r.Code == TeacherRegistrationRequirementCodes.Certificate);
        if (certReq != null && request.Certificates.Count > certReq.MaxCount)
            return $"Maximum {certReq.MaxCount} certificates allowed.";

        return null;
    }
}
