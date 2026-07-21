using Microsoft.AspNetCore.Identity;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationStatusService : ITeacherRegistrationStatusService
{
    private readonly ITeacherRegistrationRequirementRepository _requirementRepository;
    private readonly ITeacherRegistrationSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherSubjectRepository _subjectRepository;
    private readonly ITeacherRegistrationService _registrationService;
    private readonly UserManager<User> _userManager;

    public TeacherRegistrationStatusService(
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRepository teacherRepository,
        ITeacherRegistrationCompletionService completionService,
        ITeacherAvailabilityRepository availabilityRepository,
        ITeacherSubjectRepository subjectRepository,
        ITeacherRegistrationService registrationService,
        UserManager<User> userManager)
    {
        _requirementRepository = requirementRepository;
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _teacherRepository = teacherRepository;
        _completionService = completionService;
        _availabilityRepository = availabilityRepository;
        _subjectRepository = subjectRepository;
        _registrationService = registrationService;
        _userManager = userManager;
    }

    public async Task<TeacherRegistrationStatusResponseDto> GetStatusForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var flags = await BuildAccountFlagsAsync(teacherId, cancellationToken);
        var requirements = await GetChecklistForTeacherAsync(teacherId, cancellationToken);
        var legacy = await _documentRepository.GetDocumentsStatusAsync(teacherId);
        var subjectSnapshot = await _subjectRepository.GetSubjectActivationSnapshotAsync(teacherId);

        return new TeacherRegistrationStatusResponseDto
        {
            TeacherStatus = flags.TeacherStatus,
            IsAccountActivated = flags.IsAccountActivated,
            CanBeActivated = flags.CanBeActivated,
            AwaitingFinalApproval = flags.AwaitingFinalApproval,
            RequiresAvailabilitySetup = flags.RequiresAvailabilitySetup,
            SubjectSummary = MapSubjectSummary(subjectSnapshot),
            Requirements = requirements,
            LegacyDocuments = legacy
        };
    }

    public async Task<TeacherAccountStatusResponseDto> GetAccountStatusForTeacherAsync(
        int teacherId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var flags = await BuildAccountFlagsAsync(teacherId, cancellationToken);
        var nextStep = await _registrationService.GetNextRegistrationStepAsync(userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());

        return new TeacherAccountStatusResponseDto
        {
            TeacherStatus = flags.TeacherStatus,
            IsAccountActivated = flags.IsAccountActivated,
            CanBeActivated = flags.CanBeActivated,
            AwaitingFinalApproval = flags.AwaitingFinalApproval,
            RequiresAvailabilitySetup = flags.RequiresAvailabilitySetup,
            HasAcceptedTerms = user?.TermsAcceptedAt != null,
            NextStep = nextStep
        };
    }

    private async Task<AccountFlags> BuildAccountFlagsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            throw new InvalidOperationException($"Teacher {teacherId} not found.");

        var canBeActivated = await _completionService.CanActivateTeacherAccountAsync(teacherId, cancellationToken);
        var isActivated = teacher.Status == TeacherStatus.Active;
        var requiresAvailability = isActivated
            && !await _availabilityRepository.HasAnyAvailabilityAsync(teacherId);

        return new AccountFlags(
            teacher.Status,
            isActivated,
            canBeActivated,
            !isActivated && canBeActivated,
            requiresAvailability);
    }

    private sealed record AccountFlags(
        TeacherStatus TeacherStatus,
        bool IsAccountActivated,
        bool CanBeActivated,
        bool AwaitingFinalApproval,
        bool RequiresAvailabilitySetup);

    public async Task<List<TeacherRegistrationSubmissionStatusDto>> GetChecklistForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var active = await _requirementRepository.GetActiveOrderedAsync(cancellationToken);
        var submissions = await _submissionRepository.GetByTeacherIdWithRequirementsAsync(teacherId, cancellationToken);

        var result = new List<TeacherRegistrationSubmissionStatusDto>();

        foreach (var req in active)
        {
            var related = submissions.Where(s => s.RequirementId == req.Id).ToList();
            var isMultiFile = req.RequirementType == RegistrationRequirementType.File && req.MaxCount > 1;

            if (isMultiFile)
            {
                var count = related.Count;
                var worst = related.OrderByDescending(s => (int)s.VerificationStatus).FirstOrDefault();
                result.Add(new TeacherRegistrationSubmissionStatusDto
                {
                    Code = req.Code,
                    NameAr = req.NameAr,
                    NameEn = req.NameEn,
                    RequirementType = req.RequirementType.ToString(),
                    IsRequired = req.IsRequired,
                    IsSubmitted = count >= req.MinCount,
                    VerificationStatus = worst?.VerificationStatus,
                    RejectionReason = worst?.RejectionReason
                });
            }
            else
            {
                var sub = related.FirstOrDefault();
                result.Add(new TeacherRegistrationSubmissionStatusDto
                {
                    Code = req.Code,
                    NameAr = req.NameAr,
                    NameEn = req.NameEn,
                    RequirementType = req.RequirementType.ToString(),
                    IsRequired = req.IsRequired,
                    IsSubmitted = sub != null,
                    VerificationStatus = sub?.VerificationStatus,
                    RejectionReason = sub?.RejectionReason,
                    TeacherDocumentId = sub?.TeacherDocumentId,
                    TextValue = sub?.TextValue,
                    BoolValue = sub?.BoolValue,
                    SelectedOptions = ResolveSelectedOptions(req, sub)
                });
            }
        }

        return result;
    }

    private static TeacherSubjectSummaryDto MapSubjectSummary(TeacherSubjectActivationSnapshot snapshot) =>
        new()
        {
            TotalSubjects = snapshot.Total,
            PendingSubjects = snapshot.Pending,
            RejectedSubjects = snapshot.Rejected,
            ActiveSubjects = snapshot.Approved
        };

    /// <summary>
    /// For Selection submissions, splits the comma-joined <c>TextValue</c> back into the chosen
    /// option values and pairs each with its bilingual label from the requirement's OptionsJson.
    /// Returns null for non-Selection requirements (or when nothing was submitted yet).
    /// </summary>
    private static List<RequirementOptionDto>? ResolveSelectedOptions(
        TeacherRegistrationRequirement req,
        TeacherRegistrationSubmission? sub)
    {
        if (req.RequirementType != RegistrationRequirementType.Selection)
            return null;
        if (sub == null || string.IsNullOrWhiteSpace(sub.TextValue))
            return new List<RequirementOptionDto>();

        var picked = sub.TextValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var catalog = RegistrationRequirementOptionsHelper.Parse(req.OptionsJson);

        return picked.Select(value =>
        {
            var hit = catalog.FirstOrDefault(o => string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase));
            return new RequirementOptionDto
            {
                Value = value,
                LabelAr = hit?.LabelAr ?? value,
                LabelEn = hit?.LabelEn ?? value
            };
        }).ToList();
    }
}
