using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationService : ITeacherRegistrationService
{
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<TeacherRegistrationService> _logger;
    private readonly ITeacherSubjectRepository _subjectRepository;
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly IAuthLoginOtpHelper _authLoginOtpHelper;
    private readonly ITeacherLifecycleEmailService _lifecycleEmailService;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherReviewCorrectionService _reviewCorrectionService;
    private readonly ITeacherDomainQuestionStatusService _domainQuestionStatusService;

    public TeacherRegistrationService(
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        IAuthenticationService authService,
        ILogger<TeacherRegistrationService> logger,
        ITeacherSubjectRepository subjectRepository,
        ITeacherAvailabilityRepository availabilityRepository,
        IAuthLoginOtpHelper authLoginOtpHelper,
        ITeacherLifecycleEmailService lifecycleEmailService,
        ITeacherRegistrationCompletionService completionService,
        ITeacherReviewCorrectionService reviewCorrectionService,
        ITeacherDomainQuestionStatusService domainQuestionStatusService)
    {
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _authService = authService;
        _logger = logger;
        _subjectRepository = subjectRepository;
        _availabilityRepository = availabilityRepository;
        _authLoginOtpHelper = authLoginOtpHelper;
        _lifecycleEmailService = lifecycleEmailService;
        _completionService = completionService;
        _reviewCorrectionService = reviewCorrectionService;
        _domainQuestionStatusService = domainQuestionStatusService;
    }

    public async Task<PhoneVerificationDto> CreateBasicAccountAsync(
        string fullPhoneNumber,
        string? email = null,
        DateTime? termsAcceptedAt = null)
    {
        var accountEmail = _authLoginOtpHelper.ResolveAccountEmail(email, fullPhoneNumber);

        var user = new User
        {
            UserName = fullPhoneNumber,
            PhoneNumber = fullPhoneNumber,
            PhoneNumberConfirmed = true,
            Email = accountEmail,
            NormalizedEmail = accountEmail.ToUpperInvariant(),
            IsActive = false,
            EmailConfirmed = false,
            TermsAcceptedAt = termsAcceptedAt
        };

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user account: {Errors}", errors);
            throw new Exception($"Failed to create account: {errors}");
        }

        // Assign Teacher role
        await _userManager.AddToRoleAsync(user, Roles.Teacher);

        // Generate JWT token
        var jwtResult = await _authService.GetJWTToken(user);

        _logger.LogInformation("New teacher account created for user {UserId}", user.Id);

        return new PhoneVerificationDto
        {
            UserId = user.Id,
            Token = jwtResult.AccessToken,
            PhoneNumber = fullPhoneNumber
        };
    }

    public async Task<TeacherAccountDto> CompleteAccountAsync(
        int userId,
        string firstName,
        string lastName,
        string? email,
        string password)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new Exception("User not found");
        }

        await EnsureTeacherRoleForUserAsync(user);

        // Email uniqueness check — only when the email actually changes for this user.
        if (!string.IsNullOrWhiteSpace(email))
        {
            var trimmedEmail = email.Trim();
            var sameAsCurrent = !string.IsNullOrEmpty(user.Email)
                && string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase);
            if (!sameAsCurrent)
            {
                User? emailOwner;
                try
                {
                    emailOwner = await _userManager.FindByEmailAsync(trimmedEmail);
                }
                catch (InvalidOperationException)
                {
                    // Legacy duplicates already in the DB — treat as a hard collision.
                    throw new Exception("Email is already registered.");
                }
                if (emailOwner != null && emailOwner.Id != user.Id)
                {
                    throw new Exception("Email is already registered.");
                }
            }
            user.Email = trimmedEmail;
            user.NormalizedEmail = trimmedEmail.ToUpperInvariant();
        }
        else if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new Exception(
                "Email is required. Include email in this request or restart registration with email OTP.");
        }
        // When email is omitted, keep the address set during OTP (step 1–2).

        // Update user details
        user.FirstName = firstName;
        user.LastName = lastName;
        user.IsActive = true;

        // Set password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var passwordResult = await _userManager.ResetPasswordAsync(user, token, password);

        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
            throw new Exception($"Failed to set password: {errors}");
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            // Most common failure now is the Identity "DuplicateEmail" validator (RequireUniqueEmail).
            // Surface the message so the caller (handler) translates it to a 400.
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new Exception($"Failed to update account: {errors}");
        }

        // Create Teacher profile when this is the first personal-info completion.
        var existingTeacher = await _teacherRepository.GetByUserIdAsync(user.Id);
        if (existingTeacher == null)
        {
            var teacher = new Teacher
            {
                UserId = user.Id,
                Status = TeacherStatus.AwaitingDocuments,
                IsActive = true
            };

            await _teacherRepository.AddAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Teacher account completed for user {UserId}, teacher {TeacherId}",
                user.Id,
                teacher.Id);
        }
        else
        {
            _logger.LogInformation(
                "Teacher profile already exists for user {UserId} during personal-info completion",
                user.Id);
        }

        // Generate new JWT token with updated user info (includes Teacher role).
        var jwtResult = await _authService.GetJWTToken(user);

        return new TeacherAccountDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber!,
            Token = jwtResult.AccessToken
        };
    }

    public async Task EnsureTeacherRoleForUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new Exception("User not found");

        await EnsureTeacherRoleForUserAsync(user);
    }

    private async Task EnsureTeacherRoleForUserAsync(User user)
    {
        if (await _userManager.IsInRoleAsync(user, Roles.Teacher))
            return;

        var result = await _userManager.AddToRoleAsync(user, Roles.Teacher);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to assign Teacher role: {errors}");
        }

        _logger.LogInformation("Assigned Teacher role to user {UserId} during teacher registration", user.Id);
    }

    public async Task CompleteDocumentUploadAsync(
        int teacherId,
        bool isInSaudiArabia)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        var previousStatus = teacher?.Status ?? TeacherStatus.AwaitingDocuments;

        // Update teacher status to PendingVerification
        await _teacherRepository.UpdateStatusAsync(
            teacherId,
            TeacherStatus.PendingVerification);

        // Update teacher location
        var location = isInSaudiArabia
            ? TeacherLocation.InsideSaudiArabia
            : TeacherLocation.OutsideSaudiArabia;

        await _teacherRepository.UpdateLocationAsync(teacherId, location);

        await _teacherRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Document upload completed for teacher {TeacherId}, status set to PendingVerification",
            teacherId);

        if (previousStatus == TeacherStatus.AwaitingDocuments)
            await _lifecycleEmailService.SendRegistrationReceivedAsync(teacherId);
    }

    public async Task<RegistrationStepDto> GetNextRegistrationStepAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new Exception("User not found");
        }

        // Check if Teacher profile exists
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);

        if (teacher == null)
        {
            // Step 2 complete, need Step 3
            return new RegistrationStepDto
            {
                CurrentStep = 2,
                NextStep = 3,
                NextStepName = "Complete Personal Information",
                IsRegistrationComplete = false
            };
        }

        // Teacher profile exists, check status
        switch (teacher.Status)
        {
            case TeacherStatus.AwaitingDocuments:
                return new RegistrationStepDto
                {
                    CurrentStep = 3,
                    NextStep = 4,
                    NextStepName = "Upload Documents",
                    IsRegistrationComplete = false
                };

            case TeacherStatus.PendingVerification:
                return await BuildPostRegistrationStepAsync(teacher.Id);

            case TeacherStatus.DocumentsRejected:
            {
                var corrections = await _reviewCorrectionService.GetPendingCorrectionsAsync(teacher.Id);

                var registrationRejected = corrections
                    .Where(c => c.Type == TeacherReviewCorrectionType.RegistrationDocument
                                && !string.IsNullOrWhiteSpace(c.RejectionReason))
                    .ToList();
                if (registrationRejected.Count > 0)
                    return BuildReuploadRejectedDocumentsStep(registrationRejected);

                if (await _domainQuestionStatusService.HasRejectedDomainQuestionsAsync(teacher.Id))
                {
                    var domainRejected = await _domainQuestionStatusService.GetRejectedDomainCorrectionsAsync(teacher.Id);
                    return BuildFixDomainVerificationStep(domainRejected);
                }

                return await BuildPostRegistrationStepAsync(teacher.Id);
            }

            case TeacherStatus.Active:
            {
                if (await NeedsRegistrationActionBeforeSubjectsAsync(teacher.Id))
                    return await BuildPostRegistrationStepAsync(teacher.Id);

                var hasSubjects = await _subjectRepository.HasAnySubjectOfferingsAsync(teacher.Id);
                if (!hasSubjects)
                    return BuildAddSubjectsStep();

                if (await ShouldOfferSetAvailabilityStepAsync(teacher.Id))
                    return BuildSetAvailabilityStep();

                return new RegistrationStepDto
                {
                    CurrentStep = 6,
                    NextStep = 0,
                    NextStepName = "Dashboard",
                    IsRegistrationComplete = true,
                    RequiresAvailabilitySetup = false,
                    Message = "Welcome back! Your teacher dashboard is ready."
                };
            }

            case TeacherStatus.Blocked:
                throw new Exception("Your account has been blocked. Please contact support.");

            default:
                throw new Exception("Unknown registration status");
        }
    }

    private async Task<RegistrationStepDto> BuildPostRegistrationStepAsync(int teacherId)
    {
        var corrections = await _reviewCorrectionService.GetPendingCorrectionsAsync(teacherId);

        var registrationRejected = corrections
            .Where(c => c.Type == TeacherReviewCorrectionType.RegistrationDocument
                        && !string.IsNullOrWhiteSpace(c.RejectionReason))
            .ToList();
        if (registrationRejected.Count > 0)
            return BuildReuploadRejectedDocumentsStep(registrationRejected);

        if (await _domainQuestionStatusService.HasRejectedDomainQuestionsAsync(teacherId))
        {
            var domainRejected = await _domainQuestionStatusService.GetRejectedDomainCorrectionsAsync(teacherId);
            return BuildFixDomainVerificationStep(domainRejected);
        }

        var catalogDomainIds = await _domainQuestionStatusService.GetCatalogDomainIdsWithRequiredQuestionsAsync();
        if (catalogDomainIds.Count > 0
            && await _domainQuestionStatusService.HasIncompleteCatalogDomainAnswersAsync(teacherId))
            return BuildCompleteDomainQuestionsStep();

        if (catalogDomainIds.Count > 0
            && await _domainQuestionStatusService.HasAnyAnswersPendingAdminReviewAsync(teacherId))
            return BuildAwaitingDomainVerificationStep();

        var hasPendingRegistration =
            await _completionService.HasPendingRequiredRegistrationReviewAsync(teacherId);
        var hasPendingDomain = catalogDomainIds.Count > 0
            && await _domainQuestionStatusService.HasCatalogDomainsPendingAdminReviewAsync(teacherId);
        if (hasPendingRegistration || hasPendingDomain)
        {
            if (await ShouldOfferAddSubjectsStepAsync(teacherId, CancellationToken.None))
                return BuildAddSubjectsStep();

            if (await ShouldOfferSetAvailabilityStepAsync(teacherId))
                return BuildSetAvailabilityStep();

            return BuildAwaitingDomainVerificationStep();
        }

        if (await _completionService.CanActivateTeacherAccountAsync(teacherId))
        {
            if (await ShouldOfferAddSubjectsStepAsync(teacherId, CancellationToken.None))
                return BuildAddSubjectsStep();

            if (await ShouldOfferSetAvailabilityStepAsync(teacherId))
                return BuildSetAvailabilityStep();

            return BuildAwaitingFinalApprovalStep();
        }

        if (await ShouldOfferAddSubjectsStepAsync(teacherId, CancellationToken.None))
            return BuildAddSubjectsStep();

        if (await ShouldOfferSetAvailabilityStepAsync(teacherId))
            return BuildSetAvailabilityStep();

        return BuildAwaitingDomainVerificationStep();
    }

    private async Task<bool> NeedsRegistrationActionBeforeSubjectsAsync(int teacherId) =>
        await _domainQuestionStatusService.HasRejectedDomainQuestionsAsync(teacherId)
        || await _domainQuestionStatusService.HasIncompleteCatalogDomainAnswersAsync(teacherId)
        || await _domainQuestionStatusService.HasAnyAnswersPendingAdminReviewAsync(teacherId);

    private async Task<bool> ShouldOfferAddSubjectsStepAsync(int teacherId, CancellationToken cancellationToken) =>
        !await NeedsRegistrationActionBeforeSubjectsAsync(teacherId)
        && await _domainQuestionStatusService.HasAnyFullyApprovedCatalogDomainAsync(teacherId, cancellationToken)
        && !await _subjectRepository.HasAnySubjectOfferingsAsync(teacherId);

    private async Task<bool> ShouldOfferSetAvailabilityStepAsync(int teacherId) =>
        await _subjectRepository.HasAnySubjectOfferingsAsync(teacherId)
        && !await _availabilityRepository.HasAnyAvailabilityAsync(teacherId);

    private static RegistrationStepDto BuildReuploadRejectedDocumentsStep(
        List<TeacherReviewCorrectionDto> corrections) =>
        new()
        {
            CurrentStep = 4,
            NextStep = 4,
            NextStepName = "Re-upload Rejected Documents",
            IsRegistrationComplete = false,
            Message = "Some of your registration documents were rejected. Please check the rejection reasons and re-upload.",
            PendingCorrections = corrections
        };

    private static RegistrationStepDto BuildFixDomainVerificationStep(List<TeacherReviewCorrectionDto> corrections) =>
        new()
        {
            CurrentStep = 5,
            NextStep = 5,
            NextStepName = "Fix Domain Verification",
            IsRegistrationComplete = false,
            Message = "One or more domain verification answers were rejected. Please update them on the domain questions screen.",
            PendingCorrections = corrections
        };

    private static RegistrationStepDto BuildCompleteDomainQuestionsStep() =>
        new()
        {
            CurrentStep = 4,
            NextStep = 5,
            NextStepName = "Complete Domain Questions",
            IsRegistrationComplete = false,
            Message = "Complete the required domain verification questions for each education domain."
        };

    private static RegistrationStepDto BuildAwaitingDomainVerificationStep() =>
        new()
        {
            CurrentStep = 5,
            NextStep = 0,
            NextStepName = "Awaiting Domain Verification",
            IsRegistrationComplete = false,
            Message = "Your registration documents and domain answers are being reviewed. Please wait for admin approval."
        };

    private static RegistrationStepDto BuildAddSubjectsStep() =>
        new()
        {
            CurrentStep = 4,
            NextStep = 5,
            NextStepName = "Add Teaching Subjects and Units",
            IsRegistrationComplete = false,
            Message = "Add the subjects and content units you can teach."
        };

    private static RegistrationStepDto BuildSetAvailabilityStep() =>
        new()
        {
            CurrentStep = 5,
            NextStep = 6,
            NextStepName = "Set Your Availability",
            IsRegistrationComplete = false,
            RequiresAvailabilitySetup = true,
            Message = "Set the days and hours you are available to teach."
        };

    private static RegistrationStepDto BuildAwaitingFinalApprovalStep() =>
        new()
        {
            CurrentStep = 5,
            NextStep = 0,
            NextStepName = "Awaiting Final Approval",
            IsRegistrationComplete = false,
            AwaitingFinalApproval = true,
            Message = "Your registration documents and domain answers are approved. Waiting for final account activation by admin."
        };

}
