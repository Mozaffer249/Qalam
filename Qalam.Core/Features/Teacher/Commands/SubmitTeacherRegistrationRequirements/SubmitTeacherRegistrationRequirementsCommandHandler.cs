using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Validators;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;

public class SubmitTeacherRegistrationRequirementsCommandHandler : ResponseHandler,
    IRequestHandler<SubmitTeacherRegistrationRequirementsCommand, Response<TeacherRegistrationSubmitResponseDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationRequirementRepository _requirementRepository;
    private readonly INationalityRepository _nationalityRepository;
    private readonly ITeacherRegistrationSubmitService _submitService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly UserManager<User> _userManager;
    private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
    private readonly ILogger<SubmitTeacherRegistrationRequirementsCommandHandler> _logger;

    public SubmitTeacherRegistrationRequirementsCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationRequirementRepository requirementRepository,
        INationalityRepository nationalityRepository,
        ITeacherRegistrationSubmitService submitService,
        ITeacherRegistrationService teacherRegistrationService,
        UserManager<User> userManager,
        IStringLocalizer<SharedResources> sharedLocalizer,
        IStringLocalizer<AuthenticationResources> authLocalizer,
        ILogger<SubmitTeacherRegistrationRequirementsCommandHandler> logger) : base(sharedLocalizer)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _requirementRepository = requirementRepository;
        _nationalityRepository = nationalityRepository;
        _submitService = submitService;
        _teacherRegistrationService = teacherRegistrationService;
        _userManager = userManager;
        _authLocalizer = authLocalizer;
        _logger = logger;
    }

    public async Task<Response<TeacherRegistrationSubmitResponseDto>> Handle(
        SubmitTeacherRegistrationRequirementsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<TeacherRegistrationSubmitResponseDto>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return BadRequest<TeacherRegistrationSubmitResponseDto>("Teacher profile not found. Please complete personal information first.");

        await _teacherRegistrationService.EnsureTeacherRoleForUserAsync(request.UserId);

        if (teacher.Status == TeacherStatus.PendingVerification)
            return BadRequest<TeacherRegistrationSubmitResponseDto>(_authLocalizer[AuthenticationResourcesKeys.DocumentsAlreadyPendingVerification]);
        if (teacher.Status == TeacherStatus.Active)
            return BadRequest<TeacherRegistrationSubmitResponseDto>(_authLocalizer[AuthenticationResourcesKeys.AccountAlreadyVerified]);

        var nationalityCode = request.NationalityCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(nationalityCode))
            return BadRequest<TeacherRegistrationSubmitResponseDto>("Nationality is required.");

        var nationality = await _nationalityRepository.GetByCodeAsync(nationalityCode, cancellationToken);
        if (nationality == null || !nationality.IsActive)
            return BadRequest<TeacherRegistrationSubmitResponseDto>("Invalid or inactive nationality.");

        request.NationalityCode = nationalityCode;

        if (!request.Location.HasValue
            || (request.Location != TeacherLocation.InsideSaudiArabia
                && request.Location != TeacherLocation.OutsideSaudiArabia))
        {
            return BadRequest<TeacherRegistrationSubmitResponseDto>("Residence location is required.");
        }

        var location = request.Location.Value;
        var insideSaudi = TeacherDocumentBusinessRules.IsInsideSaudiArabia(location);

        // Issuing country applies only to foreign (outside-SA) identity documents.
        if (insideSaudi)
            request.IssuingCountryCode = null;
        else if (!string.IsNullOrWhiteSpace(request.IssuingCountryCode))
            request.IssuingCountryCode = request.IssuingCountryCode.Trim().ToUpperInvariant();

        var activeRequirements = await _requirementRepository.GetActiveOrderedAsync(cancellationToken);
        if (activeRequirements.Count == 0)
            return BadRequest<TeacherRegistrationSubmitResponseDto>("No active registration requirements configured.");

        var validationError = ValidateAgainstRequirements(request, activeRequirements);
        if (validationError != null)
            return BadRequest<TeacherRegistrationSubmitResponseDto>(validationError);

        var identityRequired = activeRequirements.Any(r =>
            r.Code == TeacherRegistrationRequirementCodes.IdentityDocument && r.IsRequired);
        if (identityRequired && request.IdentityDocumentFile != null)
        {
            try
            {
                TeacherDocumentBusinessRules.ValidateIdentityLocationRules(
                    location, request.IdentityType, request.IssuingCountryCode, _authLocalizer);

                await TeacherDocumentBusinessRules.ValidateIdentityUnique(
                    _documentRepository, request.IdentityType, request.DocumentNumber,
                    request.IssuingCountryCode, _authLocalizer, excludeTeacherId: teacher.Id);
            }
            catch (ValidationException vex)
            {
                return BadRequest<TeacherRegistrationSubmitResponseDto>(vex.Message);
            }
        }

        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user != null)
            {
                user.Nationality = nationalityCode;
                await _userManager.UpdateAsync(user);
            }

            await _submitService.SubmitAsync(teacher, MapToInput(request), activeRequirements, cancellationToken);
            await _teacherRegistrationService.CompleteDocumentUploadAsync(teacher.Id, location);

            var nextStep = await _teacherRegistrationService.GetNextRegistrationStepAsync(request.UserId);
            return Success(
                entity: new TeacherRegistrationSubmitResponseDto
                {
                    Message = "Registration submitted successfully. Complete domain verification questions to continue.",
                    NextStep = nextStep
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SubmitRegistrationRequirements handler caught teacherId={TeacherId}, userId={UserId}",
                teacher.Id, request.UserId);
            return BadRequest<TeacherRegistrationSubmitResponseDto>(ex.Message);
        }
    }

    private static TeacherRegistrationSubmissionInput MapToInput(SubmitTeacherRegistrationRequirementsCommand request) =>
        new()
        {
            NationalityCode = request.NationalityCode,
            Bio = request.Bio,
            Location = request.Location!.Value,
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

            // Legacy location requirement — ignore if still present in DB; nationality replaced it.
            if (req.Code == TeacherRegistrationRequirementCodes.Location)
                continue;

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
